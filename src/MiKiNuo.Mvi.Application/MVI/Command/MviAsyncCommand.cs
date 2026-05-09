using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示异步 MVI 命令，继承自 MviCommandBase，封装异步执行委托。
/// </summary>
public sealed class MviAsyncCommand : MviCommandBase, IMviAsyncCommand
{
    private readonly Func<object?, CancellationToken, ValueTask> _executeAsync;

    /// <summary>
    /// 初始化 MviAsyncCommand 实例。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="executeAsync">异步执行委托。</param>
    public MviAsyncCommand(Observable<bool> canExecute, Func<object?, CancellationToken, ValueTask> executeAsync)
        : base(canExecute)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    }

    /// <inheritdoc />
    public override async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter, CancellationToken.None);
    }

    /// <inheritdoc />
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        return CanExecute(parameter)
            ? _executeAsync(parameter, cancellationToken)
            : ValueTask.CompletedTask;
    }
}