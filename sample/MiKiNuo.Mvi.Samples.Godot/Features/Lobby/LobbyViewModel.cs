using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅组合根 ViewModel。
/// <para>
/// 3 个常驻子 VM（玩家头部 / 大厅菜单 / 活动日志）由构造函数注入并暴露为只读属性；
/// 5 个互斥面板 VM 通过 <see cref="ILobbyPanelFactory"/> 按 <see cref="CurrentPanel"/> 解析。
/// 父 <see cref="LobbyState"/> 不再持有任何 <c>*ViewModel</c> 引用。
/// </para>
/// </summary>
public sealed partial class LobbyViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    private readonly ILobbyPanelFactory _panelFactory;

    /// <summary>
    /// 初始化游戏大厅组合根 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="playerHeaderViewModel">玩家头部子组件 ViewModel（常驻）。</param>
    /// <param name="lobbyMenuViewModel">大厅菜单子组件 ViewModel（常驻）。</param>
    /// <param name="activityLogViewModel">活动日志子组件 ViewModel（常驻）。</param>
    /// <param name="panelFactory">互斥面板 ViewModel 工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public LobbyViewModel(
        IMviStore<LobbyState, LobbyIntent, LobbyEffect> store,
        PlayerHeaderViewModel playerHeaderViewModel,
        LobbyMenuViewModel lobbyMenuViewModel,
        ActivityLogViewModel activityLogViewModel,
        ILobbyPanelFactory panelFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(playerHeaderViewModel);
        ArgumentNullException.ThrowIfNull(lobbyMenuViewModel);
        ArgumentNullException.ThrowIfNull(activityLogViewModel);
        ArgumentNullException.ThrowIfNull(panelFactory);

        PlayerHeaderViewModel = playerHeaderViewModel;
        LobbyMenuViewModel = lobbyMenuViewModel;
        ActivityLogViewModel = activityLogViewModel;
        _panelFactory = panelFactory;
    }

    /// <summary>获取当前大厅面板键。</summary>
    [MviBind(nameof(LobbyState.CurrentPanel))]
    public partial string CurrentPanel { get; private set; }

    /// <summary>获取玩家头部 ViewModel（构造函数注入，Shell 生命周期内静态）。</summary>
    public PlayerHeaderViewModel PlayerHeaderViewModel { get; }

    /// <summary>获取大厅菜单 ViewModel（构造函数注入，Shell 生命周期内静态）。</summary>
    public LobbyMenuViewModel LobbyMenuViewModel { get; }

    /// <summary>获取活动日志 ViewModel（构造函数注入，Shell 生命周期内静态）。</summary>
    public ActivityLogViewModel ActivityLogViewModel { get; }

    /// <summary>
    /// 按 <see cref="CurrentPanel"/> 通过 <see cref="ILobbyPanelFactory"/> 解析当前可见面板的 ViewModel。
    /// <para>
    /// 5 个互斥面板（任务大厅 / 英雄队伍 / 背包仓库 / 锻造工坊 / 战斗准备）共用同一份 <c>LobbyStore</c>，
    /// 由工厂内部缓存避免每次切换面板都重新构造；未识别 <see cref="CurrentPanel"/> 时返回 null。
    /// </para>
    /// </summary>
    /// <returns>当前可见面板的 ViewModel；未识别 <see cref="CurrentPanel"/> 时返回 null。</returns>
    public object? CreateCurrentPanelViewModel()
    {
        return _panelFactory.CreatePanel(CurrentPanel);
    }
}
