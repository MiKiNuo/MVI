using System.Windows.Input;
using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 命令的抽象基类，封装了可执行状态管理、R3 订阅与 UI 通知的公共逻辑。
/// </summary>
public abstract class MviCommandBase : ICommand, IDisposable
{
    private readonly IDisposable _canExecuteSubscription;
    private bool _canExecute;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI 命令基类。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    protected MviCommandBase(Observable<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(canExecute);

        _canExecuteSubscription = canExecute.Subscribe(this, static (value, command) => command.SetCanExecute(value));
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !_isDisposed && _canExecute;
    }

    /// <inheritdoc />
    public abstract void Execute(object? parameter);

    /// <inheritdoc />
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
    /// 在 UI 线程上触发 CanExecuteChanged 事件。
    /// </summary>
    protected void RaiseCanExecuteChanged()
    {
        MviUiNotificationDispatcher.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
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
}