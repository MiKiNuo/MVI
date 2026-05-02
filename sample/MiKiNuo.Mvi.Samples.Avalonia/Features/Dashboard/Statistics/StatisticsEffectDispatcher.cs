using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件副作用分发器。
/// </summary>
public sealed class StatisticsEffectDispatcher : IMviEffectDispatcher<StatisticsEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(StatisticsEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
