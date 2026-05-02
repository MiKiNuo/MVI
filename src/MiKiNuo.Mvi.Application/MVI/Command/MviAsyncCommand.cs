using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示由 R3 可执行流驱动的异步命令。
/// </summary>
public sealed class MviAsyncCommand : IMviAsyncCommand, IDisposable
{
    private readonly Func<object?, CancellationToken, ValueTask> _executeAsync;
    private readonly IDisposable _canExecuteSubscription;
    private bool _canExecute;
    private bool _isDisposed;

    /// <summary>
    /// 初始化由 R3 可执行流驱动的异步命令。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="executeAsync">命令执行委托。</param>
    public MviAsyncCommand(
        Observable<bool> canExecute,
        Func<object?, CancellationToken, ValueTask> executeAsync)
    {
        ArgumentNullException.ThrowIfNull(canExecute);
        ArgumentNullException.ThrowIfNull(executeAsync);

        _executeAsync = executeAsync;
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
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        return CanExecute(parameter)
            ? _executeAsync(parameter, cancellationToken)
            : ValueTask.CompletedTask;
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
