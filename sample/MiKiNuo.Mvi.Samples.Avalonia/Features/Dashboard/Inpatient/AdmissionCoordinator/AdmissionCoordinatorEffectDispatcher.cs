using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI 副作用分发器。
/// </summary>
public sealed class AdmissionCoordinatorEffectDispatcher : IMviEffectDispatcher<AdmissionCoordinatorEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化入院流程 MVI 副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public AdmissionCoordinatorEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(AdmissionCoordinatorEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is AdmissionCoordinatorEffect.RequestAdmissionRegistration admissionRegistration)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "入院流程 MVI", "提交入院登记", admissionRegistration.ContextText,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is AdmissionCoordinatorEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "入院流程 MVI", DashboardComponentActionKeys.Primary, primaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is AdmissionCoordinatorEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "入院流程 MVI", DashboardComponentActionKeys.Secondary, secondaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
