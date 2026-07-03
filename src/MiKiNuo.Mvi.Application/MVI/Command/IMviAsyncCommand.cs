namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 异步命令。
/// </summary>
/// <remarks>
/// 继承自 <see cref="IMviCommand"/> 以便平台层（Godot 绑定、Avalonia 内置 <c>Button.Command</c>）
/// 接受任一种命令时不用关心同步/异步差异。
/// <see cref="IMviCommand"/> 已继承 <see cref="System.Windows.Input.ICommand"/>,
/// <see cref="ExecuteAsync"/> 暴露 MVI 的异步执行契约,平台无关。
/// </remarks>
public interface IMviAsyncCommand : IMviCommand
{
    /// <summary>
    /// 异步执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步执行过程的任务。</returns>
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default);
}
