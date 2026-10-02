using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

internal static class WorkspaceVerification
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    internal static async Task<string> RunAsync(WorkspaceWindow window)
    {
        Require(window.TryGetPlatformHandle()?.Handle != IntPtr.Zero && Dispatcher.UIThread.CheckAccess(), "真实 HWND 与 UI 线程");
        Require(window.First.InstanceId != window.Second.InstanceId && window.First.Snapshot.State.ObjectId == window.Second.Snapshot.State.ObjectId,
            "同类同业务对象多开具有独立实例身份");
        Require(window.Root.Children.Active.Count == 2 && window.First.Children.Active.Count == 1, "完整嵌套实例所有权与业务 State 分离");
        OperationResult<System.Collections.Immutable.ImmutableArray<string>> coordinated = await window.Root.CoordinateAsync(window.First.Load, window.Second.Load);
        Require(coordinated.Kind == OperationResultKind.Completed && coordinated.Value.Length == 2, "明确逐端口协调多个目标");
        window.First.SetText("left edit");
        Require(window.Second.Snapshot.State.Text == "coordinated", "两个状态独立");
        Require((await window.Mediator.SendAsync<WorkspaceLoad, string>(new(7, "ambiguous"))).Kind == RequestResultKind.AmbiguousTarget,
            "多候选不隐式选择");
        PostReceipt<string> detail = window.DetailMediator.Post(new WorkspaceLoad(7, "nested detail"), window.Detail.Load);
        Require((await detail.Completion!.WaitAsync(Watchdog)).Value == "nested detail", "嵌套详情仅经定向契约处理");
        TextBox oldDetailInput = window.Panels.GetVisualDescendants().OfType<TextBox>().Last();
        window.RemoveEditor(window.First);
        oldDetailInput.Text = "removed detail input";
        Require(window.Detail.Snapshot.State.Text == "nested detail", "移除父编辑器同步断开详情输入");
        Require(window.Second.Snapshot.State.Text == "coordinated", "移除父和详情不影响其他宿主");
        window.First.Close();
        Require(window.Root.Children.Active.Count == 1 && window.First.IsClosed && window.Detail.IsClosed, "子直接关闭统一退出活动树并关闭后代");
        Require((await window.Mediator.SendAsync(new WorkspaceLoad(7, "old"), window.First.Load)).Kind == RequestResultKind.TargetUnavailable,
            "旧地址拒绝");
        Require((await window.Mediator.SendAsync<WorkspaceLoad, string>(new(7, "remaining"))).OperationResult!.Value == "remaining",
            "剩余唯一目标自动恢复");
        WorkspaceEditorFeature added = window.AddEditor();
        Require(window.Root.Children.Active.Count == 2 && added.InstanceId != window.First.InstanceId, "动态添加新实例");
        window.RemoveEditor(added);

        await using WorkspaceService independentService = new();
        WorkspaceEditorFeature independent = new(independentService);
        Mediator standalone = new();
        using IDisposable standaloneRoute = standalone.Register(independent.Load);
        Require((await standalone.SendAsync<WorkspaceLoad, string>(new(7, "standalone"))).OperationResult!.Value == "standalone",
            "同一业务Feature可以独立宿主运行");
        await independent.Close().Ticket.Released.WaitAsync(Watchdog);

        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        WorkspaceService? scoped = null;
        ServiceCollection services = new();
        services.AddScoped(_ => scoped = new WorkspaceService(async (text, _) =>
        {
            entered.SetResult();
            return await release.Task;
        }));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        WorkspaceEditorFeature pending = await FeatureFactory.CreateAsync<WorkspaceEditorFeature>(provider);
        FeatureMember member = window.Root.Children.Add(pending);
        member.Wire(window.Mediator, pending.Load);
        Task<RequestResult<string>> operation = window.Mediator.SendAsync(new WorkspaceLoad(7, "pending"), pending.Load);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult childClosed = member.Remove();
            Require(!childClosed.Ticket.Released.IsCompleted && !scoped!.IsDisposed && window.Root.Children.Retired.Count != 0,
                "退休scoped子资源继续由父保留");
            CloseResult parentClosed = window.Root.Close();
            Require(!parentClosed.Ticket.Released.IsCompleted, "父关闭仍等待之前退休的子执行");
            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
            release.SetResult("late");
            Require((await operation.WaitAsync(Watchdog)).OperationResult!.Kind == OperationResultKind.Canceled, "迟到反馈明确取消");
            Require((await parentClosed.Ticket.Released.WaitAsync(Watchdog)).Succeeded && scoped!.IsDisposed,
                "最后真实退出后父票据与子范围释放");
        }
        finally
        {
            release.TrySetResult("cleanup");
            await window.Root.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        return "PASS v2-workspace: real Windows HWND/UI thread; same-type same-business-ID isolated editors; nested independent detail; explicit multi-target Mediator coordination and Post; child direct Close/Remove update active routes; old address rejected and unique route restored; dynamic Add/Remove; standalone reuse; retired uncooperative scoped IO remains owned; parent Released waits until final real exit.";
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
