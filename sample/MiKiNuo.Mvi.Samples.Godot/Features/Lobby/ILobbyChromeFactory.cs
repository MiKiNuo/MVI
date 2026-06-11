namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 3 个常驻子组件 ViewModel（玩家头部 / 大厅菜单 / 活动日志）的工厂。
/// <para>
/// 父 <see cref="LobbyViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="LobbyViewModel.CreateHeaderViewModel"/>、
/// <see cref="LobbyViewModel.CreateMenuViewModel"/>、
/// <see cref="LobbyViewModel.CreateActivityLogViewModel"/> 等方法按需解析。
/// </para>
/// <para>
/// 与 <see cref="ILobbyPanelFactory"/> 区分：<see cref="ILobbyPanelFactory"/> 负责 5 个互斥面板
/// （任务大厅 / 英雄队伍 / 背包仓库 / 锻造工坊 / 战斗准备），本工厂负责"Shell 生命周期内静态不变"的
/// 玩家头部 / 大厅菜单 / 活动日志 3 个 chrome 子组件。
/// </para>
/// </summary>
public interface ILobbyChromeFactory
{
    /// <summary>
    /// 解析玩家头部子组件 ViewModel。
    /// </summary>
    /// <returns>玩家头部 <c>PlayerHeaderViewModel</c> 实例（缓存）。</returns>
    public object CreateHeaderViewModel();

    /// <summary>
    /// 解析大厅菜单子组件 ViewModel。
    /// </summary>
    /// <returns>大厅菜单 <c>LobbyMenuViewModel</c> 实例（缓存）。</returns>
    public object CreateMenuViewModel();

    /// <summary>
    /// 解析活动日志子组件 ViewModel。
    /// </summary>
    /// <returns>活动日志 <c>ActivityLogViewModel</c> 实例（缓存）。</returns>
    public object CreateActivityLogViewModel();
}
