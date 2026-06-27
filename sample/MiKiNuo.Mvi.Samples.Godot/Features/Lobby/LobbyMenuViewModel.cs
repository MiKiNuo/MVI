using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using R3;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅菜单 ViewModel。
/// </summary>
public sealed partial class LobbyMenuViewModel : MviViewModelBase<NavigationState, NavigationIntent, NavigationEffect>
{
    /// <summary>
    /// 初始化大厅菜单 ViewModel。
    /// </summary>
    /// <param name="store">导航状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public LobbyMenuViewModel(
        IMviStore<NavigationState, NavigationIntent, NavigationEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        Observable<bool> canExecute = Store.States
            .Select(static _ => true)
            .DistinctUntilChanged();

        SelectMissionBoardCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new NavigationIntent.SelectPanel(LobbyPanel.MissionBoard), ct), UiDispatcher);
        SelectHeroRosterCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new NavigationIntent.SelectPanel(LobbyPanel.HeroRoster), ct), UiDispatcher);
        SelectInventoryCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new NavigationIntent.SelectPanel(LobbyPanel.Inventory), ct), UiDispatcher);
        SelectForgeLabCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new NavigationIntent.SelectPanel(LobbyPanel.ForgeLab), ct), UiDispatcher);
        SelectBattlePrepCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new NavigationIntent.SelectPanel(LobbyPanel.BattlePrep), ct), UiDispatcher);
    }

    /// <summary>获取当前面板。</summary>
    [MviBind(nameof(NavigationState.CurrentPanel))]
    public partial LobbyPanel CurrentPanel { get; private set; }

    /// <summary>获取选择任务大厅命令。</summary>
    public MviAsyncCommand SelectMissionBoardCommand { get; }

    /// <summary>获取选择英雄队伍命令。</summary>
    public MviAsyncCommand SelectHeroRosterCommand { get; }

    /// <summary>获取选择背包仓库命令。</summary>
    public MviAsyncCommand SelectInventoryCommand { get; }

    /// <summary>获取选择锻造工坊命令。</summary>
    public MviAsyncCommand SelectForgeLabCommand { get; }

    /// <summary>获取选择战斗准备命令。</summary>
    public MviAsyncCommand SelectBattlePrepCommand { get; }

    /// <summary>获取退出登录命令。</summary>
    [MviCommand(typeof(NavigationIntent.Logout))]
    public partial IMviCommand LogoutCommand { get; private set; }

    /// <summary>释放手动创建的命令资源。</summary>
    protected override void OnDispose()
    {
        SelectMissionBoardCommand.Dispose();
        SelectHeroRosterCommand.Dispose();
        SelectInventoryCommand.Dispose();
        SelectForgeLabCommand.Dispose();
        SelectBattlePrepCommand.Dispose();
        base.OnDispose();
    }
}
