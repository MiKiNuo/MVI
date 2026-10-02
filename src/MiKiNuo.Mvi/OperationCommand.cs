using System.Windows.Input;

namespace MiKiNuo.Mvi;

/// <summary>将生成操作入口适配为本地 View 的原生命令。</summary>
/// <typeparam name="TResult">业务操作的返回值类型。</typeparam>
public sealed class OperationCommand<TResult> : ICommand
{
    private Func<bool>? canExecute;
    private Func<Task<OperationResult<TResult>>>? execute;

    internal OperationCommand(Func<bool> canExecute, Func<Task<OperationResult<TResult>>> execute)
    {
        this.canExecute = canExecute;
        this.execute = execute;
    }

    /// <summary>获取最近一次命令调用的实际操作任务；完成不等待 UI 展示。</summary>
    public Task<OperationResult<TResult>>? Execution { get; private set; }

    /// <summary>在所属 View 展示新快照后通知按钮重新读取可执行反馈。</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>读取已展示状态的反馈；实际执行仍进入统一启动验证。</summary>
    /// <param name="parameter">原生命令参数，此操作不需要参数。</param>
    /// <returns>当前已展示状态是否允许启动。</returns>
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? false;

    /// <summary>直接调用生成操作入口，即使反馈禁用也执行实际启动验证。</summary>
    /// <param name="parameter">原生命令参数，此操作不需要参数。</param>
    public void Execute(object? parameter)
    {
        ObjectDisposedException.ThrowIf(execute is null, this);
        Execution = execute();
    }

    internal void NotifyChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    internal void Detach()
    {
        canExecute = null;
        execute = null;
        CanExecuteChanged = null;
    }
}
