using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务大厅 ViewModel。
/// </summary>
public sealed partial class MissionBoardViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化任务大厅 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public MissionBoardViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取当前选中任务。</summary>
    [MviBind(nameof(LobbyState.SelectedMission))]
    public partial string SelectedMission { get; private set; }

    /// <summary>获取任务进度说明。</summary>
    [MviBind(nameof(LobbyState.MissionProgress))]
    public partial string MissionProgress { get; private set; }

    /// <summary>获取金币数量。</summary>
    [MviBind(nameof(LobbyState.Gold))]
    public partial int Gold { get; private set; }

    /// <summary>获取体力值。</summary>
    [MviBind(nameof(LobbyState.Stamina))]
    public partial int Stamina { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取接受森林任务命令。</summary>
    [MviCommand(typeof(LobbyIntent.AcceptForestMission), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand AcceptForestMissionCommand { get; private set; }

    /// <summary>获取接受矿洞任务命令。</summary>
    [MviCommand(typeof(LobbyIntent.AcceptMineMission), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand AcceptMineMissionCommand { get; private set; }

    /// <summary>获取完成任务命令。</summary>
    [MviCommand(typeof(LobbyIntent.CompleteMission), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand CompleteMissionCommand { get; private set; }
}
