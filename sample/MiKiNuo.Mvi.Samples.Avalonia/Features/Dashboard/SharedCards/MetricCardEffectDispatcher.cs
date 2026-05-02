using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片副作用分发器。
/// </summary>
public sealed class MetricCardEffectDispatcher : IMviEffectDispatcher<MetricCardEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(MetricCardEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
