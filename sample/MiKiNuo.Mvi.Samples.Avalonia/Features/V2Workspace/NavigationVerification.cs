using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

/// <summary>保存后的不可变导航请求，生命周期协调由宿主按实例ID解析。</summary>
/// <param name="RequesterId">请求关闭的组合实例身份。</param>
/// <param name="Destination">业务目标路由。</param>
public sealed record NavigationRequest(Guid RequesterId, string Destination);

/// <summary>导航业务响应，不等待发起者资源物理退出。</summary>
/// <param name="CloseKind">宿主请求关闭的结论。</param>
/// <param name="Destination">已经应用的目标路由。</param>
public sealed record NavigationReply(CloseRequestKind CloseKind, string Destination);

/// <summary>保存导航已经应用的业务路由。</summary>
/// <param name="Destination">已经应用的目标路由。</param>
internal sealed record NavigationState(string? Destination = null);
/// <summary>保存已完成的保存业务事实。</summary>
/// <param name="Saved">保存是否已提交。</param>
internal sealed record SaveNavigationState(bool Saved = false);

internal sealed partial class NavigationFeature : Feature<NavigationState>
{
    internal NavigationFeature(Func<Guid, Task<CloseRequestResult>> closeOwner) : base(new())
    {
        Navigate = CreateRequestPort<NavigationRequest, NavigationReply>("Navigate", null, async (operation, request) =>
        {
            CloseRequestResult closed = await closeOwner(request.RequesterId);
            if (closed.Kind == CloseRequestKind.Closed)
            {
                await operation.UpdateAsync(static (state, destination) => state with { Destination = destination }, request.Destination);
            }
            return new NavigationReply(closed.Kind, request.Destination);
        });
    }

    internal RequestPort<NavigationRequest, NavigationReply> Navigate { get; }
}

internal sealed class SaveNavigationService
{
    internal Guid RequesterId { get; set; }
    internal Mediator Mediator { get; } = new();
    internal RequestPort<NavigationRequest, NavigationReply>? Target { get; set; }
    internal TaskCompletionSource ResponseObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource ReleaseMethod { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource ReleaseChild { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Disposed { get; set; }
}

/// <summary>保持保存执行与子工作使用的 scoped 资源。</summary>
/// <param name="service">宿主持有的受控调用记录。</param>
internal sealed class SaveNavigationResource(SaveNavigationService service) : IAsyncDisposable
{
    internal void Use() => ObjectDisposedException.ThrowIf(service.Disposed, this);
    /// <summary>记录保存服务范围真实释放。</summary>
    /// <returns>范围释放任务。</returns>
    public ValueTask DisposeAsync() { service.Disposed = true; return ValueTask.CompletedTask; }
}

/// <summary>仅通过导航契约发送保存后请求的独立功能。</summary>
/// <param name="resource">本实例范围资源。</param>
/// <param name="service">宿主注入的契约路由与受控服务。</param>
internal sealed partial class SaveNavigationFeature(SaveNavigationResource resource, SaveNavigationService service)
    : Feature<SaveNavigationState>(new())
{
    [Operation]
    private async ValueTask<NavigationReply> SaveAndNavigateAsync(Operation<SaveNavigationState> operation)
    {
        resource.Use();
        await operation.UpdateAsync(static (state, _) => state with { Saved = true }, 0);
        operation.Track(KeepChildAsync());
        // 默认目标拥有导航；自身生命周期关闭不应取消已经接纳请求的响应等待。
        RequestResult<NavigationReply> response = await service.Mediator.SendAsync(
            new NavigationRequest(service.RequesterId, "workspace/saved"), service.Target!, CancellationToken.None);
        NavigationReply reply = response.OperationResult!.Value!;
        resource.Use();
        service.ResponseObserved.SetResult();
        await service.ReleaseMethod.Task;
        return reply;
    }

    private async Task KeepChildAsync() { await service.ReleaseChild.Task; resource.Use(); }
}

internal static class NavigationVerification
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    internal static async Task<string> RunAsync(WorkspaceWindow window)
    {
        Require(window.TryGetPlatformHandle()?.Handle != IntPtr.Zero && Dispatcher.UIThread.CheckAccess(), "真实HWND/UI线程");
        SaveNavigationService service = new() { RequesterId = window.Root.InstanceId };
        NavigationFeature target = new(id => id == window.Root.InstanceId ? window.Root.RequestCloseAsync()
            : throw new InvalidOperationException("未知请求者实例身份。"));
        using IDisposable route = service.Mediator.Register(target.Navigate);
        service.Target = target.Navigate;
        ServiceCollection services = new();
        services.AddSingleton(service);
        services.AddScoped<SaveNavigationResource>();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        SaveNavigationFeature requester = await FeatureFactory.CreateAsync<SaveNavigationFeature>(provider);
        window.Root.Children.Add(requester);
        window.Service.Confirmation = static (_, _) => ValueTask.FromResult(true);
        Task<OperationResult<NavigationReply>> save = requester.SaveAndNavigateAsync();
        try
        {
            await service.ResponseObserved.Task.WaitAsync(Watchdog);
            Require(window.Root.IsClosed && requester.IsClosed && !service.Disposed && !window.Root.Close().Ticket.Released.IsCompleted,
                "导航关闭组合后先返回响应，执行与子工作仍拥有范围");
            Require(requester.Snapshot.State.Saved && target.Snapshot.State.Destination == "workspace/saved", "保存外部效果/已提交状态与导航不回滚");
            service.ReleaseMethod.SetResult();
            Require(!window.Root.Close().Ticket.Released.IsCompleted, "父票据继续等待未合作子工作");
            service.ReleaseChild.SetResult();
            Require((await save.WaitAsync(Watchdog)).Kind == OperationResultKind.Canceled, "发起者收到响应后真实退出");
            Require((await window.Root.Close().Ticket.Released.WaitAsync(Watchdog)).Succeeded && service.Disposed,
                "最后使用者退出后范围及父票据真实释放");
            return "PASS v2-navigation: real Windows HWND/UI thread; saved child communicates only immutable requester ID/destination contract through Mediator; host lifecycle adapter confirms/closes owner tree; navigation response precedes requester exit; caller scope and uncooperative tracked child stay alive; committed save/navigation effects retained; original parent Released completes only after final real exit.";
        }
        finally
        {
            service.ReleaseMethod.TrySetResult(); service.ReleaseChild.TrySetResult();
            await window.Root.Close().Ticket.Released.WaitAsync(Watchdog);
            await target.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
