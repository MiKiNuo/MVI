using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi;

namespace PackageConsumer;

internal static class Program
{
    private static async Task Main()
    {
        ServiceCollection services = new();
        services.AddScoped<Resource>();
        services.AddSingleton<Shared>();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Editor first = await Editor.CreateAsync(provider);
        Editor second = await Editor.CreateAsync(provider);
        Require(!ReferenceEquals(first.Resource, second.Resource) && ReferenceEquals(first.Shared, second.Shared), "standard DI scope isolation");
        Require((await first.SubmitAsync()).Reason == "ValidationFailed", "operation validation");
        first.SetName(" draft ");
        RuntimeSnapshot<State> before = first.Snapshot;
        Task<OperationResult<int>> execution = first.SubmitAsync();
        Require(await first.Resource.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)) == "draft", "validated starting input");
        Require(first.Snapshot.OperationStates["SubmitAsync"].IsRunning, "running snapshot");
        first.SetName("edited during IO");
        first.Resource.Response.SetResult(7);
        OperationResult<int> result = await execution.WaitAsync(TimeSpan.FromSeconds(15));
        Require(result.Kind == OperationResultKind.Completed && result.Value == 7 && first.Snapshot.State.Total == 7
            && first.Snapshot.State.Name == "edited during IO" && !first.Snapshot.OperationStates["SubmitAsync"].IsRunning, "typed committed completion");
        Require(before.State.Name == "draft" && second.Snapshot.Version == 0 && second.Snapshot.State.Name == string.Empty,
            "immutable snapshots and independent instances");
        Mediator mediator = new();
        RequestPort<LoadMessage, int> load = first.CreatemessagePort();
        RequestPort<QueryMessage, int> query = first.CreateoperationPort();
        using IDisposable loadRegistration = mediator.Register(load);
        using IDisposable queryRegistration = mediator.Register(query);
        Require((await mediator.SendAsync(new LoadMessage(11), load)).OperationResult!.Value == 11, "handler named message");
        Require((await mediator.SendAsync(new QueryMessage(), query)).OperationResult!.Value == 11, "handler named operation");
        Require((await first.Close().Ticket.Released).Succeeded && (await second.Close().Ticket.Released).Succeeded
            && Resource.Disposed == 2, "owned scopes released exactly once");
        Require(!typeof(Feature).Assembly.GetReferencedAssemblies().Any(name => name.Name!.Contains("Avalonia", StringComparison.Ordinal)
            || name.Name.Contains("Godot", StringComparison.Ordinal) || name.Name.Contains("Generators", StringComparison.Ordinal)), "Core has no GUI or generator runtime reference");
        Console.WriteLine("PASS package Core: typed input/custom rule, immutable independent snapshots, validated asynchronous operation, current-state feedback, generated DI factories/scoped ownership, handler-name shadows, no GUI runtime references.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

public sealed record State
{
    [Input]
    public string Name { get; init; } = string.Empty;
    public int Total { get; init; }
}

public sealed record LoadMessage(int Value);
public sealed record QueryMessage;
public sealed class Shared;

public sealed class Resource : IDisposable
{
    public static int Disposed;
    public TaskCompletionSource<string> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<int> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Dispose() => Interlocked.Increment(ref Disposed);
}

public sealed partial class Editor(Resource resource, Shared shared) : Feature<State>(new())
{
    public Resource Resource { get; } = resource;
    public Shared Shared { get; } = shared;

    [OnInput(nameof(State.Name))]
    private static State Change(State state, string value) => state with { Name = value.Trim() };

    [Operation(Validate = nameof(Valid))]
    private async Task<int> SubmitAsync(Operation<State> operation)
    {
        Resource.Entered.SetResult(operation.Snapshot.Name);
        int value = await Resource.Response.Task.WaitAsync(operation.CancellationToken);
        await operation.UpdateAsync(static (state, total) => state with { Total = total }, value);
        return value;
    }

    private static bool Valid(State state) => state.Name.Length > 0;

    [RequestHandler]
    private async ValueTask<int> message(Operation<State> operation, LoadMessage message)
    {
        await operation.UpdateAsync(static (state, total) => state with { Total = total }, message.Value);
        return message.Value;
    }

    [RequestHandler]
    private ValueTask<int> operation(Operation<State> operation, QueryMessage message) => ValueTask.FromResult(operation.Snapshot.Total);
}
