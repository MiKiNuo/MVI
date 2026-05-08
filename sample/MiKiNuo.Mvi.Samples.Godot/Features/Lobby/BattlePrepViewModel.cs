using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备 ViewModel。
/// </summary>
public sealed partial class BattlePrepViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化战斗准备 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    public BattlePrepViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取当前选中任务。</summary>
    [MviBind(nameof(LobbyState.SelectedMission))]
    public partial string SelectedMission { get; private set; }

    /// <summary>获取英雄队伍战力。</summary>
    [MviBind(nameof(LobbyState.HeroTeamPower))]
    public partial int HeroTeamPower { get; private set; }

    /// <summary>获取体力值。</summary>
    [MviBind(nameof(LobbyState.Stamina))]
    public partial int Stamina { get; private set; }

    /// <summary>获取战斗准备摘要。</summary>
    [MviBind(nameof(LobbyState.BattleReadyText))]
    public partial string BattleReadyText { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取准备战斗命令。</summary>
    [MviCommand(typeof(LobbyIntent.PrepareBattle), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand PrepareBattleCommand { get; private set; }
}
