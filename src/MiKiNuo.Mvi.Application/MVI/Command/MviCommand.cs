using MiKiNuo.Mvi.Application.MVI.Threading;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示同步 MVI 命令，继承自 MviCommandBase，封装同步执行委托。
/// </summary>
public sealed class MviCommand : MviCommandBase, IMviCommand
{
    private readonly Action<object?> _execute;

    /// <summary>
    /// 初始化 MviCommand 实例。
    /// </summary>
    /// <param name="canExecute">可执行状态流。</param>
    /// <param name="execute">同步执行委托。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，缺省时使用 <see cref="MviInlineUiDispatcher.Instance"/>）。</param>
    public MviCommand(Observable<bool> canExecute, Action<object?> execute, IMviUiDispatcher? uiDispatcher = null)
        : base(canExecute, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
    }

    /// <inheritdoc />
    public override void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _execute(parameter);
    }
}