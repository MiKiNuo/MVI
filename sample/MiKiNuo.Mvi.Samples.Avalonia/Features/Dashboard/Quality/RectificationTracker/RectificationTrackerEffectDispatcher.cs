using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RectificationTracker;

/// <summary>
/// 表示整改闭环 MVI副作用分发器。
/// </summary>
public sealed class RectificationTrackerEffectDispatcher : IMviEffectDispatcher<RectificationTrackerEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化整改闭环 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public RectificationTrackerEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(RectificationTrackerEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is RectificationTrackerEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("运营质控", "整改闭环 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is RectificationTrackerEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("运营质控", "整改闭环 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
