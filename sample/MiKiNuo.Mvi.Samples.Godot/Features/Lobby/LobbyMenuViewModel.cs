using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅菜单 ViewModel。
/// </summary>
public sealed partial class LobbyMenuViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化大厅菜单 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    public LobbyMenuViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取当前大厅面板键。</summary>
    [MviBind(nameof(LobbyState.CurrentPanel))]
    public partial string CurrentPanel { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取选择任务大厅命令。</summary>
    [MviCommand(typeof(LobbyIntent.SelectMissionBoard), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand SelectMissionBoardCommand { get; private set; }

    /// <summary>获取选择英雄队伍命令。</summary>
    [MviCommand(typeof(LobbyIntent.SelectHeroRoster), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand SelectHeroRosterCommand { get; private set; }

    /// <summary>获取选择背包仓库命令。</summary>
    [MviCommand(typeof(LobbyIntent.SelectInventory), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand SelectInventoryCommand { get; private set; }

    /// <summary>获取选择锻造工坊命令。</summary>
    [MviCommand(typeof(LobbyIntent.SelectForgeLab), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand SelectForgeLabCommand { get; private set; }

    /// <summary>获取选择战斗准备命令。</summary>
    [MviCommand(typeof(LobbyIntent.SelectBattlePrep), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand SelectBattlePrepCommand { get; private set; }

    /// <summary>获取退出登录命令。</summary>
    [MviCommand(typeof(LobbyIntent.Logout), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand LogoutCommand { get; private set; }
}
