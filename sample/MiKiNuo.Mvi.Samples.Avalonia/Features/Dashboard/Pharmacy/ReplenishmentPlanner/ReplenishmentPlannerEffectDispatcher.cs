using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.ReplenishmentPlanner;

/// <summary>
/// 表示补货计划 MVI副作用分发器。
/// </summary>
public sealed class ReplenishmentPlannerEffectDispatcher : IMviEffectDispatcher<ReplenishmentPlannerEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化补货计划 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public ReplenishmentPlannerEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(ReplenishmentPlannerEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is ReplenishmentPlannerEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("药房库存", "补货计划 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is ReplenishmentPlannerEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("药房库存", "补货计划 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
