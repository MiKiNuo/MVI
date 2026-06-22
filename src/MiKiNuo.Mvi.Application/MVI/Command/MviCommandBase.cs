using System.Windows.Input;
using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 命令的抽象基类，封装了可执行状态管理、R3 订阅与 UI 通知的公共逻辑。
/// </summary>
/// <remarks>
/// 通过构造函数注入 <see cref="IMviUiDispatcher"/> 避免静态可变的全局状态：
/// 单元测试中可传入 <see cref="MviInlineUiDispatcher"/> 或自定义的调度收集器，
/// 平台层则传入对应的 <c>AvaloniaMviUiDispatcher</c>/<c>GodotMviUiDispatcher</c>。
/// </remarks>
public abstract class MviCommandBase : ICommand, IDisposable
{
    private readonly IDisposable _canExecuteSubscription;
    private readonly IMviUiDispatcher _uiDispatcher;
    private bool _canExecute;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI 命令基类，使用注入的 <see cref="IMviUiDispatcher"/> 在 UI 线程触发 <see cref="CanExecuteChanged"/>。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="uiDispatcher">UI 调度器；若为 <c>null</c> 则回退到 <see cref="MviInlineUiDispatcher.Instance"/>。</param>
    protected MviCommandBase(Observable<bool> canExecute, IMviUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(canExecute);

        _uiDispatcher = uiDispatcher ?? MviInlineUiDispatcher.Instance;
        _canExecuteSubscription = canExecute.Subscribe(this, static (value, command) => command.SetCanExecute(value));
    }

    /// <summary>
    /// 当可执行状态变化时触发。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 当异步命令执行过程中抛出未捕获异常时触发。
    /// 订阅者可将其转发至 <see cref="MiKiNuo.Mvi.Application.MVI.Diagnostics.IMviDiagnosticSink"/> 或上层 UI。
    /// </summary>
    public event EventHandler<CommandExceptionEventArgs>? UnhandledException;

    /// <summary>
    /// 判断命令是否可执行。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <returns>可执行返回 true。</returns>
    public bool CanExecute(object? parameter)
    {
        return !_isDisposed && _canExecute;
    }

    /// <summary>
    /// 执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    public abstract void Execute(object? parameter);

    /// <summary>
    /// 释放所有资源。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _canExecuteSubscription.Dispose();
        OnDispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 子类释放额外资源。
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// 在 UI 线程上触发 <see cref="CanExecuteChanged"/> 事件。
    /// </summary>
    protected void RaiseCanExecuteChanged()
    {
        _uiDispatcher.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }

    private void SetCanExecute(bool value)
    {
        if (_canExecute == value)
        {
            return;
        }

        _canExecute = value;
        RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 触发未捕获异常事件，由 <see cref="MviAsyncCommand"/> 在异步路径失败时调用。
    /// </summary>
    /// <param name="exception">未捕获的异常。</param>
    /// <param name="parameter">触发执行的命令参数。</param>
    protected void RaiseUnhandledException(Exception exception, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(exception);
        UnhandledException?.Invoke(this, new CommandExceptionEventArgs(exception, parameter));
    }
}