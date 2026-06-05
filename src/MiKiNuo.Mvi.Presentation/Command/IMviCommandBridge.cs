using System.Windows.Input;

namespace MiKiNuo.Mvi.Presentation.Command;

/// <summary>
/// 表示将 MVI 平台无关命令（<see cref="IMviCommand"/>）适配为平台原生命令
/// （<see cref="ICommand"/>）的工厂抽象。
/// </summary>
/// <remarks>
/// 平台控件（Avalonia <c>Button.Command</c>、MAUI/WPF 等）只识别 <see cref="ICommand"/>。
/// 当 View 需要把 MVI 命令绑定到平台控件的 <c>Command</c> 属性时，
/// 通过本接口获取 <see cref="ICommand"/> 适配器以完成绑定。
/// Presentation 层提供默认实现 <see cref="MviCommandBridge"/>，
/// 平台层亦可实现自定义的桥接策略（例如接入本地命令管理器）。</remarks>
public interface IMviCommandBridge
{
    /// <summary>
    /// 将 MVI 命令包装为 <see cref="ICommand"/> 适配器。
    /// </summary>
    /// <param name="command">MVI 命令实例。</param>
    /// <returns>实现 <see cref="ICommand"/> 的包装器，内部委托到 <paramref name="command"/>。</returns>
    public ICommand Adapt(IMviCommand command);
}
