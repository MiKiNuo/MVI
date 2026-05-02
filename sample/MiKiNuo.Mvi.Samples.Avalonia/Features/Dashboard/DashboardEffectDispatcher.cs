using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳副作用分发器。
/// </summary>
public sealed class DashboardEffectDispatcher : IMviEffectDispatcher<DashboardEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(DashboardEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
