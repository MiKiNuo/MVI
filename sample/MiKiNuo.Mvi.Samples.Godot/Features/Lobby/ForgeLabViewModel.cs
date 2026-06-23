using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊 ViewModel。
/// </summary>
public sealed partial class ForgeLabViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化锻造工坊 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public ForgeLabViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取锻造评分。</summary>
    [MviBind("Inventory.ForgeScore")]
    public partial int ForgeScore { get; private set; }

    /// <summary>获取矿石数量。</summary>
    [MviBind("Inventory.OreCount")]
    public partial int OreCount { get; private set; }

    /// <summary>获取水晶数量。</summary>
    [MviBind("Inventory.CrystalCount")]
    public partial int CrystalCount { get; private set; }

    /// <summary>获取英雄队伍战力。</summary>
    [MviBind("HeroRoster.HeroTeamPower")]
    public partial int HeroTeamPower { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取锻造武器命令。</summary>
    [MviCommand(typeof(LobbyIntent.ForgeWeapon), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand ForgeWeaponCommand { get; private set; }

    /// <summary>获取锻造护甲命令。</summary>
    [MviCommand(typeof(LobbyIntent.ForgeArmor), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand ForgeArmorCommand { get; private set; }
}
