using System.Windows.Input;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Presentation.Command;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Command;

/// <summary>
/// 表示 Avalonia 平台专用的 MVI 命令桥接器。
/// </summary>
/// <remarks>
/// <see cref="IMviCommand"/> 已继承 <see cref="ICommand"/>,
/// 故本桥接器与 <see cref="MviCommandBridge"/> 行为一致,
/// 仅做 null 校验后原样返回。
/// 保留独立类型以便未来接入 Avalonia 特定的命令管理逻辑。
/// </remarks>
public sealed class AvaloniaMviCommandBridge : IMviCommandBridge
{
    /// <summary>
    /// 获取 Avalonia 平台单例。
    /// </summary>
    public static AvaloniaMviCommandBridge Instance { get; } = new();

    private AvaloniaMviCommandBridge()
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
}
