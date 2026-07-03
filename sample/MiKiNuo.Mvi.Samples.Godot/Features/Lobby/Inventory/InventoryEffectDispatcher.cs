using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库副作用分发器。
/// </summary>
public sealed class InventoryEffectDispatcher : MviEffectDispatcherBase<InventoryEffect>
{
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly IMviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> _battlePrepStore;
    private readonly IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> _activityLogStore;
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化背包仓库副作用分发器。
    /// </summary>
    /// <param name="playerStore">玩家状态存储。</param>
    /// <param name="battlePrepStore">战斗准备状态存储。</param>
    /// <param name="activityLogStore">活动日志状态存储。</param>
    /// <param name="traceLogger">追踪日志记录器。</param>
    public InventoryEffectDispatcher(
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore,
        IMviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> battlePrepStore,
        IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> activityLogStore,
        ITraceEffectLogger traceLogger)
    {
        _playerStore = playerStore ?? throw new ArgumentNullException(nameof(playerStore));
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
    protected override async ValueTask DispatchCoreAsync(InventoryEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case InventoryEffect.Trace trace:
                _traceLogger.Log(trace);
                break;
            case InventoryEffect.RestoreStamina data:
                await _playerStore.DispatchAsync(new PlayerIntent.RestoreStamina(data.NewStamina), cancellationToken).ConfigureAwait(false);
                break;
            case InventoryEffect.AddGold data:
                await _playerStore.DispatchAsync(new PlayerIntent.AddGold(data.Amount), cancellationToken).ConfigureAwait(false);
                break;
            case InventoryEffect.UpdateBattleReadyText data:
                await _battlePrepStore.DispatchAsync(new BattlePrepIntent.UpdateReadyText(data.ReadyText), cancellationToken).ConfigureAwait(false);
                break;
            case InventoryEffect.LogActivity data:
                await _activityLogStore.DispatchAsync(new ActivityLogIntent.AppendEntry(data.Message), cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
