using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面副作用分发器。
/// </summary>
public sealed class OutpatientWorkstationEffectDispatcher : IMviEffectDispatcher<OutpatientWorkstationEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(OutpatientWorkstationEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
