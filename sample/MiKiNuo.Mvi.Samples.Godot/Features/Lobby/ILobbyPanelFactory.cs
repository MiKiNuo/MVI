using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅内部可切换面板 ViewModel 的工厂。
/// <para>
/// 父 <see cref="LobbyViewModel"/> 仅持有 <see cref="LobbyPanel"/> 判别器，
/// 不再在 <see cref="NavigationState"/> 中直接存放 5 个互斥面板 ViewModel 引用；
/// 当 <c>CurrentPanel</c> 变化时，View 层通过此工厂按需解析当前可见面板的 ViewModel。
/// </para>
/// <para>
/// "互斥面板"使用工厂模式是因为同一时刻只有 1 个可见、其余 4 个的 VM 长期不被访问，
/// 在父 State 中保留它们会放大"VM-in-VM"反模式（State 体积膨胀、缓存无效状态、序列化失真）。
/// </para>
/// </summary>
public interface ILobbyPanelFactory
{
    /// <summary>
    /// 根据 <paramref name="panel"/> 解析对应的面板 ViewModel。
    /// </summary>
    /// <param name="panel">面板枚举（<see cref="LobbyPanel"/> 中之一）。</param>
    /// <returns>对应面板的 ViewModel；未识别 panel 时返回 null。</returns>
    public object? CreatePanel(LobbyPanel panel);
}
