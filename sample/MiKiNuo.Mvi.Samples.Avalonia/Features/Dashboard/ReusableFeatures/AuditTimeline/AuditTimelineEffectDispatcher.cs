using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 副作用分发器。
/// </summary>
public sealed class AuditTimelineEffectDispatcher : IMviEffectDispatcher<AuditTimelineEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(AuditTimelineEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
