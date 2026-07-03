using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备副作用分发器。
/// </summary>
public sealed class BattlePrepEffectDispatcher : MviEffectDispatcherBase<BattlePrepEffect>
{
    private readonly IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> _activityLogStore;
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化战斗准备副作用分发器。
    /// </summary>
    /// <param name="activityLogStore">活动日志状态存储。</param>
    /// <param name="traceLogger">追踪日志记录器。</param>
    public BattlePrepEffectDispatcher(
        IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> activityLogStore,
        ITraceEffectLogger traceLogger)
    {
        _activityLogStore = activityLogStore ?? throw new ArgumentNullException(nameof(activityLogStore));
        _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(BattlePrepEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case BattlePrepEffect.Trace trace:
                _traceLogger.Log(trace);
                break;
            case BattlePrepEffect.LogActivity data:
                await _activityLogStore.DispatchAsync(new ActivityLogIntent.AppendEntry(data.Message), cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
