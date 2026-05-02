using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.WardRiskPanel;

/// <summary>
/// 表示病区风险 MVI副作用分发器。
/// </summary>
public sealed class WardRiskPanelEffectDispatcher : IMviEffectDispatcher<WardRiskPanelEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化病区风险 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public WardRiskPanelEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(WardRiskPanelEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is WardRiskPanelEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("住院床位", "病区风险 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is WardRiskPanelEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("住院床位", "病区风险 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
