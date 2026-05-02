using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 副作用分发器。
/// </summary>
public sealed class AuditTimelineEffectDispatcher : IMviEffectDispatcher<AuditTimelineEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(AuditTimelineEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
