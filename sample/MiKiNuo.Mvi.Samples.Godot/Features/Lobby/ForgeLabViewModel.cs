using System;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊 ViewModel。
/// </summary>
public sealed partial class ForgeLabViewModel : MviViewModelBase<UnitState, ForgeLabIntent, ForgeLabEffect>
{
    private readonly IDisposable _inventorySubscription;
    private readonly IDisposable _heroRosterSubscription;
    private int _forgeScore;
    private int _oreCount;
    private int _crystalCount;
    private int _heroTeamPower;

    /// <summary>
    /// 初始化锻造工坊 ViewModel。
    /// </summary>
    /// <param name="store">锻造工坊状态存储（无状态）。</param>
    /// <param name="inventoryStore">背包状态存储（跨 Store 读取材料/评分）。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储（跨 Store 读取战力）。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public ForgeLabViewModel(
        IMviStore<UnitState, ForgeLabIntent, ForgeLabEffect> store,
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(inventoryStore);
        ArgumentNullException.ThrowIfNull(heroRosterStore);

        _inventorySubscription = inventoryStore.States.Subscribe(this, static (state, vm) =>
        {
            vm.ForgeScore = state.ForgeScore;
            vm.OreCount = state.OreCount;
            vm.CrystalCount = state.CrystalCount;
        });
        _heroRosterSubscription = heroRosterStore.States.Subscribe(this, static (state, vm) =>
        {
            vm.HeroTeamPower = state.HeroTeamPower;
        });

        Observable<bool> canExecute = Store.States
            .Select(static _ => true)
            .DistinctUntilChanged();

        ForgeWeaponCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new ForgeLabIntent.Forge(ForgeSpec.Weapon), ct), UiDispatcher);
        ForgeArmorCommand = new MviAsyncCommand(canExecute, (_, ct) => DispatchAsync(new ForgeLabIntent.Forge(ForgeSpec.Armor), ct), UiDispatcher);
    }

    /// <summary>获取锻造评分。</summary>
    public int ForgeScore
    {
        get => _forgeScore;
        private set => SetProperty(ref _forgeScore, value);
    }

    /// <summary>获取矿石数量。</summary>
    public int OreCount
    {
        get => _oreCount;
        private set => SetProperty(ref _oreCount, value);
    }

    /// <summary>获取水晶数量。</summary>
    public int CrystalCount
    {
        get => _crystalCount;
        private set => SetProperty(ref _crystalCount, value);
    }

    /// <summary>获取英雄队伍战力。</summary>
    public int HeroTeamPower
    {
        get => _heroTeamPower;
        private set => SetProperty(ref _heroTeamPower, value);
    }

    /// <summary>获取锻造武器命令。</summary>
    public MviAsyncCommand ForgeWeaponCommand { get; }

    /// <summary>获取锻造护甲命令。</summary>
    public MviAsyncCommand ForgeArmorCommand { get; }

    /// <summary>无状态 Store，ApplyStateCore 为空操作。</summary>
    protected override void ApplyStateCore(UnitState state)
    {
    }

    /// <summary>释放跨 Store 订阅和手动命令资源。</summary>
    protected override void OnDispose()
    {
        _inventorySubscription.Dispose();
        _heroRosterSubscription.Dispose();
        ForgeWeaponCommand.Dispose();
        ForgeArmorCommand.Dispose();
        base.OnDispose();
    }
}
