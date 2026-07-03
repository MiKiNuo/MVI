using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊副作用分发器。
/// </summary>
public sealed class ForgeLabEffectDispatcher : MviEffectDispatcherBase<ForgeLabEffect>
{
    private readonly IMviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;
    private readonly IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly IMviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> _battlePrepStore;
    private readonly IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> _activityLogStore;
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化锻造工坊副作用分发器。
    /// </summary>
    /// <param name="inventoryStore">背包仓库状态存储。</param>
    /// <param name="heroRosterStore">英雄队伍状态存储。</param>
    /// <param name="battlePrepStore">战斗准备状态存储。</param>
    /// <param name="activityLogStore">活动日志状态存储。</param>
    /// <param name="traceLogger">追踪日志记录器。</param>
    public ForgeLabEffectDispatcher(
        IMviStore<InventoryState, InventoryIntent, InventoryEffect> inventoryStore,
        IMviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStore,
        IMviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> battlePrepStore,
        IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> activityLogStore,
        ITraceEffectLogger traceLogger)
    {
        _inventoryStore = inventoryStore ?? throw new ArgumentNullException(nameof(inventoryStore));
        _heroRosterStore = heroRosterStore ?? throw new ArgumentNullException(nameof(heroRosterStore));
        _battlePrepStore = battlePrepStore ?? throw new ArgumentNullException(nameof(battlePrepStore));
        _activityLogStore = activityLogStore ?? throw new ArgumentNullException(nameof(activityLogStore));
        _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(ForgeLabEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case ForgeLabEffect.Trace trace:
                _traceLogger.Log(trace);
                break;
            case ForgeLabEffect.ConsumeMaterials data:
                await _inventoryStore.DispatchAsync(new InventoryIntent.ConsumeMaterials(data.OreCost, data.CrystalCost), cancellationToken).ConfigureAwait(false);
                break;
            case ForgeLabEffect.UpdateForgeScore data:
                await _inventoryStore.DispatchAsync(new InventoryIntent.UpdateForgeScore(data.ForgeScore), cancellationToken).ConfigureAwait(false);
                break;
            case ForgeLabEffect.AddPower data:
                await _heroRosterStore.DispatchAsync(new HeroRosterIntent.AddPower(data.Bonus), cancellationToken).ConfigureAwait(false);
                break;
            case ForgeLabEffect.UpdateBattleReadyText data:
                await _battlePrepStore.DispatchAsync(new BattlePrepIntent.UpdateReadyText(data.ReadyText), cancellationToken).ConfigureAwait(false);
                break;
            case ForgeLabEffect.LogActivity data:
                await _activityLogStore.DispatchAsync(new ActivityLogIntent.AppendEntry(data.Message), cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
