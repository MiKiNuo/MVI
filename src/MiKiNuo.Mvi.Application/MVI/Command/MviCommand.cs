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
public MviCommand(Observable<bool> canExecute, Action<object?> execute)
    : base(canExecute)
{
    _execute = execute ?? throw new ArgumentNullException(nameof(execute));
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