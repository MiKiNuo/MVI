using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库 ViewModel。
/// </summary>
public sealed partial class InventoryViewModel : MviViewModelBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 初始化背包仓库 ViewModel。
    /// </summary>
    /// <param name="store">大厅状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public InventoryViewModel(IMviStore<LobbyState, LobbyIntent, LobbyEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取药水数量。</summary>
    [MviBind("Inventory.PotionCount")]
    public partial int PotionCount { get; private set; }

    /// <summary>获取矿石数量。</summary>
    [MviBind("Inventory.OreCount")]
    public partial int OreCount { get; private set; }

    /// <summary>获取水晶数量。</summary>
    [MviBind("Inventory.CrystalCount")]
    public partial int CrystalCount { get; private set; }

    /// <summary>获取金币数量。</summary>
    [MviBind("Player.Gold")]
    public partial int Gold { get; private set; }

    /// <summary>获取体力值。</summary>
    [MviBind("Player.Stamina")]
    public partial int Stamina { get; private set; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    [MviBind(nameof(LobbyState.CanExecuteCommands))]
    public partial bool CanExecuteCommands { get; private set; }

    /// <summary>获取使用药水命令。</summary>
    [MviCommand(typeof(LobbyIntent.UsePotion), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand UsePotionCommand { get; private set; }

    /// <summary>获取打开金币箱命令。</summary>
    [MviCommand(typeof(LobbyIntent.OpenGoldBox), CanExecuteProperty = nameof(CanExecuteCommands))]
    public partial IMviCommand OpenGoldBoxCommand { get; private set; }
}
