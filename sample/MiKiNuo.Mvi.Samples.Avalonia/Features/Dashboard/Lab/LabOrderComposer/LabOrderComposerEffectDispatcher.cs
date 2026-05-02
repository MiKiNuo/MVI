using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI 副作用分发器。
/// </summary>
public sealed class LabOrderComposerEffectDispatcher : IMviEffectDispatcher<LabOrderComposerEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化医嘱开立 MVI 副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public LabOrderComposerEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(LabOrderComposerEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is LabOrderComposerEffect.RequestLabOrderSubmission labOrderSubmission)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("检验医嘱", "医嘱开立 MVI", "提交检验医嘱", labOrderSubmission.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is LabOrderComposerEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("检验医嘱", "医嘱开立 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is LabOrderComposerEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("检验医嘱", "医嘱开立 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
