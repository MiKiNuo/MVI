using System.Windows.Input;
using MiKiNuo.Mvi.Application.MVI.Command;

namespace MiKiNuo.Mvi.Presentation.Command;

/// <summary>
/// 提供 <see cref="IMviCommandBridge"/> 的默认实现。
/// </summary>
/// <remarks>
/// <see cref="IMviCommand"/> 已继承 <see cref="ICommand"/>,
/// <see cref="MviCommandBase"/> 直接实现两者,
/// 故本桥接器无需包装,仅做 null 校验后原样返回。
/// </remarks>
public sealed class MviCommandBridge : IMviCommandBridge
{
    /// <summary>
    /// 获取进程级单例。
    /// </summary>
    public static MviCommandBridge Instance { get; } = new();

    private MviCommandBridge()
    {
    }

    /// <summary>
    /// 将 MVI 命令作为 <see cref="ICommand"/> 返回。
    /// </summary>
    /// <param name="command">MVI 命令实例。</param>
    /// <returns>实现 <see cref="ICommand"/> 的命令实例。</returns>
    public ICommand Adapt(IMviCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command;
    }

    /// <summary>
    /// 把指定的 MVI 命令作为 <see cref="ICommand"/> 返回,
    /// 等价于 <c>MviCommandBridge.Instance.Adapt(command)</c>。
    /// </summary>
    /// <param name="command">MVI 命令实例。</param>
    /// <returns>实现 <see cref="ICommand"/> 的命令实例。</returns>
    public static ICommand ToICommand(IMviCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command;
    }
}
