using MiKiNuo.Mvi.Binding.Threading;
using R3;
namespace MiKiNuo.Mvi.Binding.Command;
/// <summary>统一的 ValueTask 命令；同步完成不阻塞线程，执行期间原子拒绝重复操作。</summary>
public sealed class MviAsyncCommand : MviCommandBase, IMviAsyncCommand
{
    private readonly Func<object?, CancellationToken, ValueTask> _execute;
    private int _running;
    /// <summary>创建使用状态流的命令。</summary>
    /// <param name="canExecute">状态流。</param>
    /// <param name="executeAsync">业务执行委托。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public MviAsyncCommand(Observable<bool> canExecute, Func<object?, CancellationToken, ValueTask> executeAsync, IMviUiDispatcher? uiDispatcher = null)
        : base(canExecute, uiDispatcher) => _execute = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    /// <summary>创建使用绑定条件的命令，不为每个命令额外订阅整个 Store。</summary>
    /// <param name="canExecute">可执行条件。</param>
    /// <param name="executeAsync">业务执行委托。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public MviAsyncCommand(Func<bool> canExecute, Func<object?, CancellationToken, ValueTask> executeAsync, IMviUiDispatcher? uiDispatcher = null)
        : base(canExecute, uiDispatcher) => _execute = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    /// <summary>获取当前是否有操作正在执行。</summary>
    public bool IsRunning => Volatile.Read(ref _running) != 0;
    /// <summary>同时检查业务条件与原子执行状态。</summary>
    /// <param name="parameter">参数。</param>
    /// <returns>是否可执行。</returns>
    public override bool CanExecute(object? parameter) => !IsRunning && base.CanExecute(parameter);
    /// <summary>平台 void 入口：同步构造失败和异步执行失败均被观察。</summary>
    /// <param name="parameter">参数。</param>
    public override void Execute(object? parameter)
    {
        try
        {
            ValueTask execution = ExecuteAsync(parameter);
            if (!execution.IsCompletedSuccessfully) _ = ObserveAsync(execution);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { RaiseUnhandledException(exception, null); }
    }
    /// <summary>显式异步调用保留异常和取消语义；重复提交返回完成而不执行。</summary>
    /// <param name="parameter">快照构造的 payload。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>完整业务任务。</returns>
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!base.CanExecute(parameter) || Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            return ValueTask.CompletedTask;
        try
        {
            NotifyCanExecuteChanged();
            ValueTask pending = _execute(parameter, cancellationToken);
            if (pending.IsCompletedSuccessfully) { pending.GetAwaiter().GetResult(); Finish(); return ValueTask.CompletedTask; }
            return AwaitExecutionAsync(pending);
        }
        catch { Finish(); throw; }
    }
    private async ValueTask AwaitExecutionAsync(ValueTask pending)
    { try { await pending.ConfigureAwait(false); } finally { Finish(); } }
    private async Task ObserveAsync(ValueTask pending)
    {
        try { await pending.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { RaiseUnhandledException(exception, null); }
    }
    private void Finish() { Interlocked.Exchange(ref _running, 0); NotifyCanExecuteChanged(); }
}
