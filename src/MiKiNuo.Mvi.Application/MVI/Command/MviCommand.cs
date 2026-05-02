using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示由 R3 可执行流驱动的同步命令。
/// </summary>
public sealed class MviCommand : IMviCommand, IDisposable
{
    private readonly Action<object?> _execute;
    private readonly IDisposable _canExecuteSubscription;
    private bool _canExecute;
    private bool _isDisposed;

    /// <summary>
    /// 初始化由 R3 可执行流驱动的同步命令。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="execute">命令执行委托。</param>
    public MviCommand(Observable<bool> canExecute, Action<object?> execute)
    {
        ArgumentNullException.ThrowIfNull(canExecute);
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecuteSubscription = canExecute.Subscribe(this, static (value, command) => command.UpdateCanExecute(value));
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !_isDisposed && _canExecute;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute(parameter);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _canExecuteSubscription.Dispose();
        _isDisposed = true;
    }

    private void UpdateCanExecute(bool value)
    {
        if (_canExecute == value)
        {
            return;
        }

        _canExecute = value;
        RaiseCanExecuteChanged();
    }

    private void RaiseCanExecuteChanged()
    {
        MviUiNotificationDispatcher.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}
