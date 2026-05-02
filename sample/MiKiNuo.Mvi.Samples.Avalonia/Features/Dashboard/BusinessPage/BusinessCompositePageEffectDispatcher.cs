using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示通用复杂业务页面副作用分发器。
/// </summary>
public sealed class BusinessCompositePageEffectDispatcher : IMviEffectDispatcher<BusinessCompositePageEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(BusinessCompositePageEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
