using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志副作用分发器。
/// </summary>
public sealed class ActivityLogEffectDispatcher : MviEffectDispatcherBase<ActivityLogEffect>
{
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化活动日志副作用分发器。
    /// </summary>
    /// <param name="traceLogger">追踪日志记录器。</param>
    public ActivityLogEffectDispatcher(ITraceEffectLogger traceLogger)
    {
        ArgumentNullException.ThrowIfNull(traceLogger);
        _traceLogger = traceLogger;
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override ValueTask DispatchCoreAsync(ActivityLogEffect effect, CancellationToken cancellationToken)
    {
        if (effect is ActivityLogEffect.Trace trace)
        {
            _traceLogger.Log(trace);
        }

        return ValueTask.CompletedTask;
    }
}
