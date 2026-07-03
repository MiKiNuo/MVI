using System;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家头部 ViewModel。
/// </summary>
public sealed partial class PlayerHeaderViewModel : MviViewModelBase<PlayerState, PlayerIntent, PlayerEffect>
{
    private string _currentPanelTitle = string.Empty;

    /// <summary>
    /// 初始化玩家头部 ViewModel。
    /// </summary>
    /// <param name="store">玩家状态存储。</param>
    /// <param name="navigationStore">导航状态存储（跨 Store 读取面板标题）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public PlayerHeaderViewModel(
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> store,
        IMviStore<NavigationState, NavigationIntent, NavigationEffect> navigationStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(navigationStore);
        BindSiblingState(navigationStore, state =>
        {
            CurrentPanelTitle = state.CurrentPanelTitle;
        });
    }

    /// <summary>获取玩家名称。</summary>
    [MviBind(nameof(PlayerState.PlayerName))]
    public partial string PlayerName { get; private set; }

    /// <summary>获取玩家等级。</summary>
    [MviBind(nameof(PlayerState.PlayerLevel))]
    public partial int PlayerLevel { get; private set; }

    /// <summary>获取金币数量。</summary>
    [MviBind(nameof(PlayerState.Gold))]
    public partial int Gold { get; private set; }

    /// <summary>获取体力值。</summary>
    [MviBind(nameof(PlayerState.Stamina))]
    public partial int Stamina { get; private set; }

    /// <summary>获取当前面板标题。</summary>
    public string CurrentPanelTitle
    {
        get => _currentPanelTitle;
        private set => SetProperty(ref _currentPanelTitle, value);
    }
}
