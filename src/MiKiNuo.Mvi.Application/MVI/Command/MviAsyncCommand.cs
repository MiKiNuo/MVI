using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示异步 MVI 命令，继承自 MviCommandBase，封装异步执行委托。
/// </summary>
/// <remarks>
/// <see cref="Execute(object?)"/> 由 <see cref="System.Windows.Input.ICommand"/> 契约约束必须返回 <c>void</c>。
/// 因此异步执行通过 fire-and-forget 模式启动，并通过 <see cref="MviCommandBase.UnhandledException"/> 桥接未捕获异常，
/// 避免 <c>async void</c> 静默吞掉异常的经典反模式。
/// </remarks>
public sealed class MviAsyncCommand : MviCommandBase, IMviAsyncCommand
{
    private readonly Func<object?, CancellationToken, ValueTask> _executeAsync;

    /// <summary>
    /// 初始化 MviAsyncCommand 实例。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="executeAsync">异步执行委托。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，缺省时使用 <see cref="MviInlineUiDispatcher.Instance"/>）。</param>
    public MviAsyncCommand(Observable<bool> canExecute, Func<object?, CancellationToken, ValueTask> executeAsync, IMviUiDispatcher? uiDispatcher = null)
        : base(canExecute, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        _executeAsync = executeAsync;
    }

    /// <inheritdoc />
    public override void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        ValueTask task = ExecuteAsync(parameter, CancellationToken.None);

        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        // 故意 fire-and-forget：异常已在 AwaitAndForwardAsync 内部通过 UnhandledException 事件转发。
#pragma warning disable CA2012
        _ = AwaitAndForwardAsync(task, parameter);
#pragma warning restore CA2012
    }

    /// <inheritdoc />
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default)
    {
        return CanExecute(parameter)
            ? _executeAsync(parameter, cancellationToken)
            : ValueTask.CompletedTask;
    }

    private async ValueTask AwaitAndForwardAsync(ValueTask task, object? parameter)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RaiseUnhandledException(ex, parameter);
        }
    }
}
