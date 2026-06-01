using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedOverview;

/// <summary>
/// 表示床位总览 MVI副作用分发器。
/// </summary>
public sealed class BedOverviewEffectDispatcher : IMviEffectDispatcher<BedOverviewEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化床位总览 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public BedOverviewEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(BedOverviewEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is BedOverviewEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "床位总览 MVI", DashboardComponentActionKeys.Primary, primaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is BedOverviewEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "床位总览 MVI", DashboardComponentActionKeys.Secondary, secondaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
