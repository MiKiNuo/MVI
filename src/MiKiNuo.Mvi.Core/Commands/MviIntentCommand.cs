using System.Windows.Input;

namespace MiKiNuo.Mvi.Core.Commands;

/// <summary>
/// 表示由 MVI 意图驱动的命令。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
public sealed class MviIntentCommand<TViewModel, TIntent> : ICommand
{
    private readonly TViewModel owner;
    private readonly Func<TViewModel, Task> executeAsync;
    private readonly Func<TViewModel, bool>? canExecute;

    /// <summary>
    /// 初始化 MVI 意图命令。
    /// </summary>
    /// <param name="owner">命令所属的 ViewModel。</param>
    /// <param name="executeAsync">执行委托。</param>
    /// <param name="canExecute">可执行判断委托。</param>
    public MviIntentCommand(
        TViewModel owner,
        Func<TViewModel, Task> executeAsync,
        Func<TViewModel, bool>? canExecute)
    {
        this.owner = owner;
        this.executeAsync = executeAsync;
        this.canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke(owner) ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _ = executeAsync(owner);
    }

    /// <summary>
    /// 触发命令可执行状态变更通知。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
