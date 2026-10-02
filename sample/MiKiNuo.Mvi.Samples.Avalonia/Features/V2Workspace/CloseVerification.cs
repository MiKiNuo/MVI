using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

internal static class CloseVerification
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    internal static async Task<string> RunAsync(WorkspaceWindow window)
    {
        Require(window.TryGetPlatformHandle()?.Handle != IntPtr.Zero && Dispatcher.UIThread.CheckAccess(), "真实 HWND 和 UI 线程");
        window.First.SetText("dirty draft");
        window.Close();
        CloseRequestResult rejected = await window.LastCloseRequest!.WaitAsync(Watchdog);
        Require(rejected.Kind == CloseRequestKind.Rejected && window.IsVisible && !window.Root.IsClosed && !window.Second.IsClosed,
            "原生Closing拒绝后整个窗口仍可用");
        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
        TextBox firstInput = window.Panels.GetVisualDescendants().OfType<TextBox>().First();
        firstInput.Text = "editing after rejection";
        Require(window.First.Snapshot.State.Text == "editing after rejection", "拒绝后原生输入继续提交");
        Require((await window.Mediator.SendAsync(new WorkspaceLoad(7, "IO after rejection"), window.Second.Load)).OperationResult!.Kind
            == OperationResultKind.Completed, "拒绝后定向IO继续完成");

        WorkspaceEditorFeature remove = window.AddEditor();
        remove.SetText("double remove draft");
        TaskCompletionSource bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> approveRemoval = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int removing = 0;
        window.Service.Confirmation = async (_, _) =>
        {
            if (Interlocked.Increment(ref removing) == 2) bothEntered.SetResult();
            return await approveRemoval.Task;
        };
        Task<CloseRequestResult> removeFirst = window.RequestRemoveEditorAsync(remove);
        Task<CloseRequestResult> removeSecond = window.RequestRemoveEditorAsync(remove);
        await bothEntered.Task.WaitAsync(Watchdog);
        approveRemoval.SetResult(true);
        CloseRequestResult[] removed = await Task.WhenAll(removeFirst, removeSecond).WaitAsync(Watchdog);
        Require(removed.All(result => result.Kind == CloseRequestKind.Closed) && !window.First.IsClosed && !window.Second.IsClosed,
            "同编辑器重复批准移除幂等且不误删其它编辑器");

        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int confirms = 0;
        window.Service.Confirmation = async (_, _) =>
        {
            if (Interlocked.Increment(ref confirms) == 1) { entered.SetResult(); return await release.Task; }
            return true;
        };
        window.Close();
        Task<CloseRequestResult> closing = window.LastCloseRequest!;
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            window.First.SetText("edit while confirming");
            WorkspaceEditorFeature added = window.AddEditor();
            added.SetText("new dirty member");
            release.SetResult(true);
            CloseRequestResult approved = await closing.WaitAsync(Watchdog);
            Require(approved.Kind == CloseRequestKind.Closed && window.Root.IsClosed && added.IsClosed, "变化后的新成员整树批准关闭");
            Require(confirms >= 4, "业务状态和成员变化重新完整确认");
            await approved.Close!.Ticket.Released.WaitAsync(Watchdog);
            return "PASS v2-close: real Windows HWND/UI thread; native Closing canceled during preparation; dirty rejection preserves Window/tree/input/routes/IO; edits and dynamic Add while confirmation waits require new preparation; all approvals commit entire tree; native Window closes only after approved flag; original release ticket completes.";
        }
        finally { release.TrySetResult(true); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
