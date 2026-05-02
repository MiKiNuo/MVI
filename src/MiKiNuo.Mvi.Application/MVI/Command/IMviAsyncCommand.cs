using System.Windows.Input;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 异步命令。
/// </summary>
public interface IMviAsyncCommand : ICommand
{
    /// <summary>
    /// 异步执行命令。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步执行过程的任务。</returns>
    public ValueTask ExecuteAsync(object? parameter, CancellationToken cancellationToken = default);
}
