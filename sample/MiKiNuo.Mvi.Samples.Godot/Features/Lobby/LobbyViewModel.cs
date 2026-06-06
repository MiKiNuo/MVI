using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅组合根 ViewModel。
/// </summary>
public sealed partial class LobbyViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化游戏大厅组合根 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public LobbyViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>获取当前大厅面板键。</summary>
    [MviBind(nameof(LobbyState.CurrentPanel))]
    public partial string CurrentPanel { get; private set; }

    /// <summary>获取玩家头部 ViewModel。</summary>
    [MviBind(nameof(LobbyState.PlayerHeaderViewModel))]
    public partial PlayerHeaderViewModel? PlayerHeaderViewModel { get; private set; }

    /// <summary>获取大厅菜单 ViewModel。</summary>
    [MviBind(nameof(LobbyState.LobbyMenuViewModel))]
    public partial LobbyMenuViewModel? LobbyMenuViewModel { get; private set; }

    /// <summary>获取任务大厅 ViewModel。</summary>
    [MviBind(nameof(LobbyState.MissionBoardViewModel))]
    public partial MissionBoardViewModel? MissionBoardViewModel { get; private set; }

    /// <summary>获取英雄队伍 ViewModel。</summary>
    [MviBind(nameof(LobbyState.HeroRosterViewModel))]
    public partial HeroRosterViewModel? HeroRosterViewModel { get; private set; }

    /// <summary>获取背包仓库 ViewModel。</summary>
    [MviBind(nameof(LobbyState.InventoryViewModel))]
    public partial InventoryViewModel? InventoryViewModel { get; private set; }

    /// <summary>获取锻造工坊 ViewModel。</summary>
    [MviBind(nameof(LobbyState.ForgeLabViewModel))]
    public partial ForgeLabViewModel? ForgeLabViewModel { get; private set; }

    /// <summary>获取战斗准备 ViewModel。</summary>
    [MviBind(nameof(LobbyState.BattlePrepViewModel))]
    public partial BattlePrepViewModel? BattlePrepViewModel { get; private set; }

    /// <summary>获取活动日志 ViewModel。</summary>
    [MviBind(nameof(LobbyState.ActivityLogViewModel))]
    public partial ActivityLogViewModel? ActivityLogViewModel { get; private set; }
}
