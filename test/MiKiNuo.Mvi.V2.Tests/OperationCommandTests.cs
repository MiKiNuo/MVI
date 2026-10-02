using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证生成的 UI 命令复用操作入口与本地快照展示。</summary>
public sealed class OperationCommandTests
{
    /// <summary>禁用反馈不代替启动验证，命令等待不依赖展示。</summary>
    /// <returns>命令执行验证任务。</returns>
    [Test]
    public async Task CommandExecutesSameValidationAndDoesNotWaitForDisplay()
    {
        TaskCompletionSource<string> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectedOperationFeature feature = new(started, response.Task);
        ConcurrentQueue<Action> callbacks = new();
        using ProjectedOperationFeature.Projection projection = feature.CreateProjection(callbacks.Enqueue);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        projection.SubmitAsyncCommand.Execute(null);
        OperationResult<int> rejected = await projection.SubmitAsyncCommand.Execution!;
        await Assert.That(rejected.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(started.Task.IsCompleted).IsFalse();
        feature.SetName("ready");
        projection.SubmitAsyncCommand.Execute(null);
        await Assert.That(await started.Task.WaitAsync(TimeSpan.FromSeconds(10))).IsEqualTo("ready");
        response.SetResult(7);
        OperationResult<int> completed = await projection.SubmitAsyncCommand.Execution!.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(projection.Snapshot.State.Result).IsEqualTo(0);
        while (callbacks.TryDequeue(out Action? callback))
        {
            callback();
        }

        await Assert.That(projection.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsTrue();
    }

    /// <summary>纯验证异常只禁用按钮反馈，实际调用返回故障，释放后不再执行。</summary>
    /// <returns>命令验证故障与释放验证任务。</returns>
    [Test]
    public async Task ValidationFaultAndDisposedProjectionAreHandled()
    {
        CommandRuleFeature feature = new();
        feature.SetName("fault");
        using CommandRuleFeature.Projection projection = feature.CreateProjection(static callback => callback());
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        projection.SubmitAsyncCommand.Execute(null);
        await Assert.That((await projection.SubmitAsyncCommand.Execution!).Kind).IsEqualTo(OperationResultKind.Faulted);
        int changed = 0;
        projection.SubmitAsyncCommand.CanExecuteChanged += (_, _) => changed++;
        feature.SetName("ready");
        await Assert.That(changed).IsEqualTo(1);
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsTrue();
        projection.Dispose();
        long before = feature.Snapshot.Version;
        Exception? failure = null;
        try { projection.SubmitAsyncCommand.Execute(null); }
        catch (Exception exception) { failure = exception; }
        await Assert.That(failure is ObjectDisposedException).IsTrue();
        await Assert.That(projection.SubmitAsyncCommand.CanExecute(null)).IsFalse();
        await Assert.That(feature.Snapshot.Version).IsEqualTo(before);
        feature.SetName("after-dispose");
        await Assert.That(changed).IsEqualTo(1);
    }
    /// <summary>按钮反馈遵循声明的替代、等待和并行名额，执行仍进入统一操作入口。</summary>
    /// <param name="concurrency">显式选择的操作策略。</param>
    /// <returns>命令并发策略验证任务。</returns>
    [Test]
    [Arguments(OperationConcurrency.Latest)]
    [Arguments(OperationConcurrency.Queue)]
    [Arguments(OperationConcurrency.Parallel)]
    public async Task CommandFeedbackAllowsDeclaredConcurrency(OperationConcurrency concurrency)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyCommandFeature feature = new(async operation =>
        {
            entered.TrySetResult();
            return await response.Task.WaitAsync(operation.CancellationToken);
        });
        using PolicyCommandFeature.Projection projection = feature.CreateProjection(static callback => callback());
        OperationCommand<int> command = concurrency switch
        {
            OperationConcurrency.Latest => projection.LatestAsyncCommand,
            OperationConcurrency.Queue => projection.QueueAsyncCommand,
            _ => projection.ParallelAsyncCommand,
        };
        command.Execute(null);
        Task<OperationResult<int>> first = command.Execution!;
        Task<OperationResult<int>>? second = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(command.CanExecute(null)).IsTrue();
            command.Execute(null);
            second = command.Execution!;
            await Assert.That(command.CanExecute(null)).IsEqualTo(concurrency == OperationConcurrency.Latest);
            response.SetResult(7);
            OperationResult<int> firstResult = await first.WaitAsync(TimeSpan.FromSeconds(10));
            OperationResult<int> secondResult = await second.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(firstResult.Kind).IsEqualTo(concurrency == OperationConcurrency.Latest
                ? OperationResultKind.Superseded : OperationResultKind.Completed);
            await Assert.That(secondResult.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(command.CanExecute(null)).IsTrue();
        }
        finally
        {
            response.TrySetResult(7);
            await first.WaitAsync(TimeSpan.FromSeconds(10));
            if (second is not null) await second.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}

internal sealed record CommandRuleState
{
    [Input]
    public string Name { get; init; } = string.Empty;
}

internal sealed partial class CommandRuleFeature() : Feature<CommandRuleState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private ValueTask<int> SubmitAsync(Operation<CommandRuleState> operation) => ValueTask.FromResult(7);

    private static bool CanSubmit(CommandRuleState state) => state.Name == "fault"
        ? throw new InvalidOperationException("PRIVATE_INPUT") : state.Name.Length != 0;
}

internal sealed partial class PolicyCommandFeature(Func<Operation<CommandRuleState>, ValueTask<int>> service)
    : Feature<CommandRuleState>(new())
{
    [Operation(Concurrency = OperationConcurrency.Latest)]
    private ValueTask<int> LatestAsync(Operation<CommandRuleState> operation) => service(operation);

    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 1)]
    private ValueTask<int> QueueAsync(Operation<CommandRuleState> operation) => service(operation);

    [Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private ValueTask<int> ParallelAsync(Operation<CommandRuleState> operation) => service(operation);
}
