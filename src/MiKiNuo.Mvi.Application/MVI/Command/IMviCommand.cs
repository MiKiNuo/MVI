namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 同步命令。
/// </summary>
/// <remarks>
/// 此接口与 <see cref="System.Windows.Input.ICommand"/> 故意解耦，避免 MVI 核心依赖
/// <c>System.Windows.Input</c>（该程序集源自 WPF 历史，平台无关性较弱）。
/// 实际执行时由 <see cref="MviCommandBase"/> 同时实现本接口与
/// <see cref="System.Windows.Input.ICommand"/>，平台层（如 Avalonia）通过
/// <c>MiKiNuo.Mvi.Presentation.Command.IMviCommandBridge</c> 适配器获取 <c>ICommand</c> 用于控件绑定。
/// <see cref="IMviAsyncCommand"/> 继承本接口以支持统一入口（同步路径触发异步执行并 fire-and-forget）。
/// </remarks>
public interface IMviCommand
{
    /// <summary>
    /// 判断命令是否可执行。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <returns>可执行返回 true。</returns>
    public bool CanExecute(object? parameter);

    /// <summary>
    /// 执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    public void Execute(object? parameter);

    /// <summary>
    /// 当 <see cref="CanExecute(object?)"/> 结果可能发生变化时触发。
    /// </summary>
    public event EventHandler? CanExecuteChanged;
}
