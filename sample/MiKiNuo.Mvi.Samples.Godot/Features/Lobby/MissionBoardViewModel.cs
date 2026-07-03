using System;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using R3;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务大厅 ViewModel。
/// </summary>
public sealed partial class MissionBoardViewModel : MviViewModelBase<MissionState, MissionIntent, MissionEffect>
{
    private int _gold;
    private int _stamina;

    /// <summary>
    /// 初始化任务大厅 ViewModel。
    /// </summary>
    /// <param name="store">任务状态存储。</param>
    /// <param name="playerStore">玩家状态存储（跨 Store 读取金币/体力）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public MissionBoardViewModel(
        IMviStore<MissionState, MissionIntent, MissionEffect> store,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(playerStore);
        BindSiblingState(playerStore, state =>
        {
            Gold = state.Gold;
            Stamina = state.Stamina;
        });

        Observable<bool> canExecute = Store.States
            .Select(static _ => true)
            .DistinctUntilChanged();

        AcceptForestMissionCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new MissionIntent.Accept(MissionSpec.ForestPatrol), ct), UiDispatcher);
        AcceptMineMissionCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new MissionIntent.Accept(MissionSpec.MineRescue), ct), UiDispatcher);
    }

    /// <summary>获取当前选中任务。</summary>
    [MviBind(nameof(MissionState.SelectedMission))]
    public partial string SelectedMission { get; private set; }

    /// <summary>获取任务进度说明。</summary>
    [MviBind(nameof(MissionState.MissionProgress))]
    public partial string MissionProgress { get; private set; }

    /// <summary>获取金币数量。</summary>
    public int Gold
    {
        get => _gold;
        private set => SetProperty(ref _gold, value);
    }

    /// <summary>获取体力值。</summary>
    public int Stamina
    {
        get => _stamina;
        private set => SetProperty(ref _stamina, value);
    }

    /// <summary>获取接受森林任务命令。</summary>
    public MviAsyncCommand AcceptForestMissionCommand { get; }

    /// <summary>获取接受矿洞任务命令。</summary>
    public MviAsyncCommand AcceptMineMissionCommand { get; }

    /// <summary>获取完成任务命令。</summary>
    [MviCommand(typeof(MissionIntent.Complete))]
    public partial IMviCommand CompleteMissionCommand { get; private set; }

    /// <summary>释放手动命令资源。</summary>
    protected override void OnDispose()
    {
        AcceptForestMissionCommand.Dispose();
        AcceptMineMissionCommand.Dispose();
        base.OnDispose();
    }
}
