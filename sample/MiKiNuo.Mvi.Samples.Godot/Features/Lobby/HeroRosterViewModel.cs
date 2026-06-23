using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍 ViewModel。
/// </summary>
public sealed partial class HeroRosterViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化英雄队伍 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public HeroRosterViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取英雄队伍战力。</summary>
    [MviBind("HeroRoster.HeroTeamPower")]
    public partial int HeroTeamPower { get; private set; }

    /// <summary>获取战士等级。</summary>
    [MviBind("HeroRoster.WarriorLevel")]
    public partial int WarriorLevel { get; private set; }

    /// <summary>获取法师等级。</summary>
    [MviBind("HeroRoster.MageLevel")]
    public partial int MageLevel { get; private set; }

    /// <summary>获取弓手等级。</summary>
    [MviBind("HeroRoster.ArcherLevel")]
    public partial int ArcherLevel { get; private set; }

    /// <summary>获取金币数量。</summary>
    [MviBind("Player.Gold")]
    public partial int Gold { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取训练战士命令。</summary>
    [MviCommand(typeof(LobbyIntent.TrainWarrior), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand TrainWarriorCommand { get; private set; }

    /// <summary>获取训练法师命令。</summary>
    [MviCommand(typeof(LobbyIntent.TrainMage), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand TrainMageCommand { get; private set; }

    /// <summary>获取训练弓手命令。</summary>
    [MviCommand(typeof(LobbyIntent.TrainArcher), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand TrainArcherCommand { get; private set; }
}
