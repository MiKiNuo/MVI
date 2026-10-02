using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Search;

internal static class SearchVerification
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    internal static async Task<string> RunAsync(SearchWindow window, ControlledSearchService service)
    {
        Require(window.TryGetPlatformHandle() is not null && window.TryGetPlatformHandle()!.Handle != IntPtr.Zero, "真实 Windows 平台窗口");
        Require(Dispatcher.UIThread.CheckAccess(), "验收从 UI 线程开始");
        int uiThread = Environment.CurrentManagedThreadId;
        List<long> displayed = [];
        List<string> changed = [];
        bool correctThread = true;
        window.Projection.PropertyChanged += (_, args) =>
        {
            correctThread &= Dispatcher.UIThread.CheckAccess() && Environment.CurrentManagedThreadId == uiThread;
            changed.Add(args.PropertyName!);
            if (args.PropertyName == nameof(window.Projection.Snapshot))
            {
                displayed.Add(window.Projection.Snapshot.Version);
            }
        };

        ControlledSearchCall first = service.Prepare("A");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "A");
        Task<OperationResult<SearchReply>> firstExecution = window.LastExecution!;
        await first.Entered.Task.WaitAsync(Watchdog);
        await FlushAsync();
        Require(window.Busy.Text == "搜索中…" && window.Progress.Value == 10, "原生加载及进度来自操作快照");

        ControlledSearchCall second = service.Prepare("B");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "B");
        Task<OperationResult<SearchReply>> secondExecution = window.LastExecution!;
        await second.Entered.Task.WaitAsync(Watchdog);
        Require(first.Token.IsCancellationRequested, "B 请求取消 A");
        window.NotesInput.SetCurrentValue(TextBox.TextProperty, "搜索期间的新备注");
        // 清空输入会拒绝新启动，但不能使 B 的合法完成失效。
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, string.Empty);
        Require((await window.LastExecution!).Kind == OperationResultKind.Rejected, "无效新尝试被统一入口拒绝");
        await second.ReportProgress!(65);
        await FlushAsync();
        Require(window.Progress.Value == 65 && window.Busy.Text == "搜索中…", "B 的运行状态仍有效");
        RuntimeSnapshot<SearchState> latest = window.Feature.Snapshot;
        Require(await RejectsSupersededAsync(first.ReportProgress!(99)), "A 进度反馈明确被取代");
        first.Response.SetResult(new SearchReply(["A 迟到结果"]));
        Require((await firstExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Superseded, "A 真实返回后报告被取代");
        Require(ReferenceEquals(latest, window.Feature.Snapshot), "A 的进度、结果与完成不提交新快照");
        await FlushAsync();
        Require(window.Busy.Text == "搜索中…" && window.Progress.Value == 65, "A 完成不能清除 B Busy");
        second.Response.SetResult(new SearchReply(["B 最新结果"]));
        OperationResult<SearchReply> completed = await secondExecution.WaitAsync(Watchdog);
        Require(completed.Kind == OperationResultKind.Completed && completed.Value!.Results[0] == "B 最新结果", "B await 返回时结果已提交");
        await FlushAsync();
        Require(window.Results.Text == "B 最新结果" && window.Busy.Text == "搜索已结束" && window.Progress.Value == 100, "原生绑定最终展示 B");
        Require(window.QueryInput.Text == string.Empty && window.NotesInput.Text == "搜索期间的新备注", "反馈保留当前输入且不重套启动验证");
        Require(window.Error.Text is null or "", "B 完成清理运行时拒绝反馈");

        ControlledSearchCall oldFault = service.Prepare("old-fault");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "old-fault");
        Task<OperationResult<SearchReply>> oldFaultExecution = window.LastExecution!;
        await oldFault.Entered.Task.WaitAsync(Watchdog);
        ControlledSearchCall currentFault = service.Prepare("current-fault");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "current-fault");
        Task<OperationResult<SearchReply>> currentFaultExecution = window.LastExecution!;
        await currentFault.Entered.Task.WaitAsync(Watchdog);
        currentFault.Response.SetException(new InvalidOperationException("最新服务故障"));
        Require((await currentFaultExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Faulted, "最新故障结构化返回");
        await FlushAsync();
        Require(window.Error.Text == "最新服务故障" && window.Busy.Text == "搜索已结束", "原生绑定展示最新错误");
        latest = window.Feature.Snapshot;
        Require(await RejectsSupersededAsync(oldFault.ReportProgress!(99)), "B 完成后旧进度仍被拒绝");
        oldFault.Response.SetException(new InvalidOperationException("旧服务故障"));
        Require((await oldFaultExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Superseded, "旧故障保持被取代身份");
        await FlushAsync();
        Require(ReferenceEquals(latest, window.Feature.Snapshot) && window.Error.Text == "最新服务故障", "旧故障不能覆盖最新错误或完成状态");

        ControlledSearchCall businessFailure = service.Prepare("fail");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "fail");
        Task<OperationResult<SearchReply>> businessExecution = window.LastExecution!;
        await businessFailure.Entered.Task.WaitAsync(Watchdog);
        businessFailure.Response.SetResult(new SearchReply([], "业务失败"));
        Require((await businessExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Completed, "业务失败值仍正常完成");
        await FlushAsync();
        Require(window.Error.Text == "业务失败", "原生绑定展示业务错误");
        changed.Clear();
        window.NotesInput.SetCurrentValue(TextBox.TextProperty, "仅修改备注");
        await FlushAsync();
        Require(changed.Contains(nameof(window.Projection.Notes)) && !changed.Contains(nameof(window.Projection.ResultsText))
            && !changed.Contains(nameof(window.Projection.Progress)), "无关输入不刷新结果与进度字段");

        // 让已出队的旧投影在 Notes 通知内遇到新用户输入，再继续写出旧 Query。
        ControlledSearchCall beforeRewrite = service.Prepare("before-rewrite");
        window.QueryInput.SetCurrentValue(TextBox.TextProperty, "before-rewrite");
        Task<OperationResult<SearchReply>> beforeRewriteExecution = window.LastExecution!;
        await beforeRewrite.Entered.Task.WaitAsync(Watchdog);
        await FlushAsync();
        ControlledSearchCall afterRewrite = service.Prepare("after-rewrite");
        bool newInputSent = false;
        window.Projection.PropertyChanged += (_, args) =>
        {
            if (!newInputSent && args.PropertyName == nameof(window.Projection.Notes))
            {
                newInputSent = true;
                window.QueryInput.SetCurrentValue(TextBox.TextProperty, "after-rewrite");
            }
        };
        window.Feature.SetQuery("stale-projection-only");
        window.NotesInput.SetCurrentValue(TextBox.TextProperty, "触发旧投影写出");
        await afterRewrite.Entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<SearchReply>> afterRewriteExecution = window.LastExecution!;
        Require(newInputSent && window.Feature.Snapshot.State.Query == "after-rewrite", "旧投影回写不覆盖新输入");
        afterRewrite.Response.SetResult(new SearchReply(["rewrite 最新结果"]));
        Require((await afterRewriteExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Completed, "通知内新搜索合法完成");
        latest = window.Feature.Snapshot;
        beforeRewrite.Response.SetResult(new SearchReply(["rewrite 旧结果"]));
        Require((await beforeRewriteExecution.WaitAsync(Watchdog)).Kind == OperationResultKind.Superseded, "被通知内输入取代的旧工作真实退出");
        await FlushAsync();
        Require(ReferenceEquals(latest, window.Feature.Snapshot) && window.QueryInput.Text == "after-rewrite", "迟到完成及旧显示写回不产生额外搜索");
        Require(service.Calls == 7, "七次真实搜索，无投影反馈重复 IO");
        Require(correctThread && displayed.Count > 0 && displayed.Zip(displayed.Skip(1)).All(static pair => pair.First < pair.Second), "有关字段通知在 UI 线程且展示版本严格单调");
        SearchWindow demo = new() { ShowInTaskbar = false, WindowState = WindowState.Minimized };
        demo.Show();
        try
        {
            // 已有同名 A 在途时，演示仍必须启动自己的 A；随后可再次重复整段演示。
            demo.QueryInput.SetCurrentValue(TextBox.TextProperty, "A");
            Task<OperationResult<SearchReply>> previous = demo.LastExecution!;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                demo.DemoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await demo.DemoExecution!.WaitAsync(Watchdog);
                Require(demo.Demonstration.Text!.Contains("B：Completed", StringComparison.Ordinal)
                    && demo.Demonstration.Text.Contains("Superseded", StringComparison.Ordinal), "真实演示按钮重复乱序返回");
                Require(demo.Feature.Snapshot.State.Results.All(static result => result.StartsWith("B", StringComparison.Ordinal)), "演示结果仍属于 B");
            }

            await previous.WaitAsync(Watchdog);
        }
        finally
        {
            demo.Close();
        }

        return "PASS v2-search: real Windows Avalonia Window; native Query/Notes inputs and result/progress/loading/error bindings; A ignores cancellation; B admission cancels A; superseded progress/result/completion while B runs; old progress/fault after latest fault; current-State edits and validation rejection retain B; business failure is Completed; stale projection rewrite cannot restart IO; 7 controlled service calls; field filtering; UI thread; monotone display versions; real demo button repeated twice, including existing A in flight.";
    }

    private static async Task<bool> RejectsSupersededAsync(ValueTask update)
    {
        try
        {
            await update;
            return false;
        }
        catch (OperationSupersededException)
        {
            return true;
        }
    }

    private static async Task FlushAsync() => await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal sealed class ControlledSearchCall
{
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<SearchReply> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Func<int, ValueTask>? ReportProgress { get; set; }
    internal CancellationToken Token { get; set; }
}

internal sealed class ControlledSearchService
{
    private readonly ConcurrentDictionary<string, ControlledSearchCall> calls = new(StringComparer.Ordinal);
    private int count;

    internal int Calls => Volatile.Read(ref count);

    internal ControlledSearchCall Prepare(string query)
    {
        ControlledSearchCall call = new();
        if (!calls.TryAdd(query, call))
        {
            throw new InvalidOperationException("验收查询重复。");
        }

        return call;
    }

    internal async ValueTask<SearchReply> SearchAsync(string query, Func<int, ValueTask> progress, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref count);
        if (!calls.TryRemove(query, out ControlledSearchCall? call))
        {
            throw new InvalidOperationException("非预期的重复或投影反馈搜索：" + query);
        }

        call.Token = cancellationToken;
        call.ReportProgress = progress;
        await progress(10);
        call.Entered.SetResult();
        return await call.Response.Task; // 外部工作刻意忽略取消，退出时机由验收屏障控制。
    }
}
