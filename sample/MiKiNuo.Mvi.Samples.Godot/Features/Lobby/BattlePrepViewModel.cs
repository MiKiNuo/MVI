using System;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using R3;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备 ViewModel。
/// </summary>
public sealed partial class BattlePrepViewModel : MviViewModelBase<BattlePrepState, BattlePrepIntent, BattlePrepEffect>
{
    private readonly IDisposable _missionSubscription;
    private readonly IDisposable _heroRosterSubscription;
    private readonly IDisposable _playerSubscription;
    private string _selectedMission = string.Empty;
    private int _heroTeamPower;
    private int _stamina;

    /// <summary>
    /// 初始化战斗准备 ViewModel。
    /// </summary>
    /// <param name="store">战斗准备状态存储。</param>
    /// <param name="missionStore">任务状态存储（跨 Store 读取选中任务）。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储（跨 Store 读取战力）。</param>
    /// <param name="playerStore">玩家状态存储（跨 Store 读取体力）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public BattlePrepViewModel(
        IMviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> store,
        IMviStore<MissionState, MissionIntent, MissionEffect> missionStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(missionStore);
        ArgumentNullException.ThrowIfNull(heroRosterStore);
        ArgumentNullException.ThrowIfNull(playerStore);

        _missionSubscription = missionStore.States.Subscribe(this, static (state, vm) =>
        {
            vm.SelectedMission = state.SelectedMission;
        });
        _heroRosterSubscription = heroRosterStore.States.Subscribe(this, static (state, vm) =>
        {
            vm.HeroTeamPower = state.HeroTeamPower;
        });
        _playerSubscription = playerStore.States.Subscribe(this, static (state, vm) =>
        {
            vm.Stamina = state.Stamina;
        });
    }

    /// <summary>获取战斗准备摘要。</summary>
    [MviBind(nameof(BattlePrepState.BattleReadyText))]
    public partial string BattleReadyText { get; private set; }

    /// <summary>获取当前选中任务。</summary>
    public string SelectedMission
    {
        get => _selectedMission;
        private set => SetProperty(ref _selectedMission, value);
    }

    /// <summary>获取英雄队伍战力。</summary>
    public int HeroTeamPower
    {
        get => _heroTeamPower;
        private set => SetProperty(ref _heroTeamPower, value);
    }

    /// <summary>获取体力值。</summary>
    public int Stamina
    {
        get => _stamina;
        private set => SetProperty(ref _stamina, value);
    }

    /// <summary>获取准备战斗命令。</summary>
    [MviCommand(typeof(BattlePrepIntent.PrepareBattle))]
    public partial IMviCommand PrepareBattleCommand { get; private set; }

    /// <summary>释放跨 Store 订阅资源。</summary>
    protected override void OnDispose()
    {
        _missionSubscription.Dispose();
        _heroRosterSubscription.Dispose();
        _playerSubscription.Dispose();
        base.OnDispose();
    }
}
