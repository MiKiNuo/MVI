using System;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库 ViewModel。
/// </summary>
public sealed partial class InventoryViewModel : MviViewModelBase<InventoryState, InventoryIntent, InventoryEffect>
{
    private int _gold;
    private int _stamina;

    /// <summary>
    /// 初始化背包仓库 ViewModel。
    /// </summary>
    /// <param name="store">背包状态存储。</param>
    /// <param name="playerStore">玩家状态存储（跨 Store 读取金币/体力）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public InventoryViewModel(
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> store,
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
    }

    /// <summary>获取药水数量。</summary>
    [MviBind(nameof(InventoryState.PotionCount))]
    public partial int PotionCount { get; private set; }

    /// <summary>获取矿石数量。</summary>
    [MviBind(nameof(InventoryState.OreCount))]
    public partial int OreCount { get; private set; }

    /// <summary>获取水晶数量。</summary>
    [MviBind(nameof(InventoryState.CrystalCount))]
    public partial int CrystalCount { get; private set; }

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

    /// <summary>获取使用药水命令。</summary>
    [MviCommand(typeof(InventoryIntent.UsePotion))]
    public partial IMviCommand UsePotionCommand { get; private set; }

    /// <summary>获取打开金币箱命令。</summary>
    [MviCommand(typeof(InventoryIntent.OpenGoldBox))]
    public partial IMviCommand OpenGoldBoxCommand { get; private set; }
}
