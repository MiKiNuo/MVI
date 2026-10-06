using System.Windows.Input;
using MiKiNuo.Mvi.Binding.Threading;
using R3;
namespace MiKiNuo.Mvi.Binding.Command;
/// <summary>统一管理命令可执行状态、错误出口及订阅生命周期。</summary>
public abstract class MviCommandBase : ICommand, IDisposable
{
    private readonly IMviUiDispatcher _ui;
    private readonly IDisposable? _subscription;
    private readonly Func<bool>? _predicate;
    private int _disposed;
    private int _enabled;
    /// <summary>创建使用状态流的命令。</summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    protected MviCommandBase(Observable<bool> canExecute, IMviUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(canExecute);
        _ui = uiDispatcher ?? MviInlineUiDispatcher.Instance;
        _subscription = canExecute.Subscribe(this, static (value, command) =>
        {
            if (Interlocked.Exchange(ref command._enabled, value ? 1 : 0) != (value ? 1 : 0))
                command.NotifyCanExecuteChanged();
        });
    }
    /// <summary>创建使用本地绑定条件的命令。</summary>
    /// <param name="canExecute">由 UI 线程读取的纯条件。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    protected MviCommandBase(Func<bool> canExecute, IMviUiDispatcher? uiDispatcher = null)
    { _enabled = 1; _predicate = canExecute ?? throw new ArgumentNullException(nameof(canExecute)); _ui = uiDispatcher ?? MviInlineUiDispatcher.Instance; }
    /// <summary>可执行性变化通知。</summary>
    public event EventHandler? CanExecuteChanged;
    /// <summary>void 平台入口中被观察的同步或异步故障；不包含正常取消。</summary>
    public event EventHandler<CommandExceptionEventArgs>? UnhandledException;
    /// <summary>判断命令可执行性；调用方应在绑定所属 UI 线程调用。</summary>
    /// <param name="parameter">命令参数。</param>
    /// <returns>是否可执行。</returns>
    public virtual bool CanExecute(object? parameter)
        => Volatile.Read(ref _disposed) == 0 && Volatile.Read(ref _enabled) != 0 && (_predicate?.Invoke() ?? true);
    /// <summary>执行平台命令入口。</summary>
    /// <param name="parameter">命令参数。</param>
    public abstract void Execute(object? parameter);
    /// <summary>请求平台重新检查命令条件；投递后的回调会再次检查生命周期。</summary>
    public void NotifyCanExecuteChanged() => _ui.Post(() =>
    { if (Volatile.Read(ref _disposed) == 0) CanExecuteChanged?.Invoke(this, EventArgs.Empty); });
    /// <summary>在 UI 上报告平台入口的故障，不在错误报告中保留敏感命令参数。</summary>
    /// <param name="exception">操作故障。</param>
    /// <param name="parameter">为了兼容命令契约接收，但不转存参数值。</param>
    protected void RaiseUnhandledException(Exception exception, object? parameter)
    {
        _ui.Post(() =>
        {
            if (Volatile.Read(ref _disposed) == 0)
                UnhandledException?.Invoke(this, new CommandExceptionEventArgs(exception, null));
        });
    }
    /// <summary>释放后立即拒绝执行，再清理订阅与额外资源。</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        List<Exception> errors = [];
        try { _subscription?.Dispose(); } catch (Exception exception) { errors.Add(exception); }
        try { OnDispose(); } catch (Exception exception) { errors.Add(exception); }
        CanExecuteChanged = null; UnhandledException = null;
        GC.SuppressFinalize(this);
        if (errors.Count != 0) throw new AggregateException("命令清理失败。", errors);
    }
    /// <summary>释放命令自身拥有的额外资源。</summary>
    protected virtual void OnDispose() { }
}
