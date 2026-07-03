using System;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using R3;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍 ViewModel。
/// </summary>
public sealed partial class HeroRosterViewModel : MviViewModelBase<HeroRosterState, HeroRosterIntent, HeroRosterEffect>
{
    private int _gold;

    /// <summary>
    /// 初始化英雄队伍 ViewModel。
    /// </summary>
    /// <param name="store">英雄队伍状态存储。</param>
    /// <param name="playerStore">玩家状态存储（跨 Store 读取金币）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public HeroRosterViewModel(
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> store,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(playerStore);
        BindSiblingState(playerStore, state =>
        {
            Gold = state.Gold;
        });

        Observable<bool> canExecute = Store.States
            .Select(static _ => true)
            .DistinctUntilChanged();

        TrainWarriorCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new HeroRosterIntent.Train(HeroKind.Warrior), ct), UiDispatcher);
        TrainMageCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new HeroRosterIntent.Train(HeroKind.Mage), ct), UiDispatcher);
        TrainArcherCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new HeroRosterIntent.Train(HeroKind.Archer), ct), UiDispatcher);
    }

    /// <summary>获取英雄队伍战力。</summary>
    [MviBind(nameof(HeroRosterState.HeroTeamPower))]
    public partial int HeroTeamPower { get; private set; }

    /// <summary>获取战士等级。</summary>
    [MviBind(nameof(HeroRosterState.WarriorLevel))]
    public partial int WarriorLevel { get; private set; }

    /// <summary>获取法师等级。</summary>
    [MviBind(nameof(HeroRosterState.MageLevel))]
    public partial int MageLevel { get; private set; }

    /// <summary>获取弓手等级。</summary>
    [MviBind(nameof(HeroRosterState.ArcherLevel))]
    public partial int ArcherLevel { get; private set; }

    /// <summary>获取金币数量。</summary>
    public int Gold
    {
        get => _gold;
        private set => SetProperty(ref _gold, value);
    }

    /// <summary>获取训练战士命令。</summary>
    public MviAsyncCommand TrainWarriorCommand { get; }

    /// <summary>获取训练法师命令。</summary>
    public MviAsyncCommand TrainMageCommand { get; }

    /// <summary>获取训练弓手命令。</summary>
    public MviAsyncCommand TrainArcherCommand { get; }

    /// <summary>释放手动命令资源。</summary>
    protected override void OnDispose()
    {
        TrainWarriorCommand.Dispose();
        TrainMageCommand.Dispose();
        TrainArcherCommand.Dispose();
        base.OnDispose();
    }
}
