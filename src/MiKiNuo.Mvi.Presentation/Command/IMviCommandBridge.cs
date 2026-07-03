using System.Windows.Input;

namespace MiKiNuo.Mvi.Presentation.Command;

/// <summary>
/// 表示将 MVI 平台无关命令（<see cref="IMviCommand"/>）适配为平台原生命令
/// （<see cref="ICommand"/>）的工厂抽象。
/// </summary>
/// <remarks>
/// <see cref="IMviCommand"/> 已继承 <see cref="ICommand"/>,
/// 默认实现 <see cref="MviCommandBridge"/> 仅做 null 校验后原样返回。
/// 保留本接口以便平台层接入自定义命令管理逻辑。
/// </remarks>
public interface IMviCommandBridge
{
    /// <summary>
    /// 将 MVI 命令包装为 <see cref="ICommand"/> 适配器。
    /// </summary>
    /// <param name="command">MVI 命令实例。</param>
    /// <returns>实现 <see cref="ICommand"/> 的包装器，内部委托到 <paramref name="command"/>。</returns>
    public ICommand Adapt(IMviCommand command);
}
