using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI 副作用分发器。
/// </summary>
public sealed class PrescriptionReviewBoardEffectDispatcher : IMviEffectDispatcher<PrescriptionReviewBoardEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化处方审核 MVI 副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public PrescriptionReviewBoardEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(PrescriptionReviewBoardEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is PrescriptionReviewBoardEffect.RequestPrescriptionReviewSubmission prescriptionReviewSubmission)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("药房库存", "处方审核 MVI", "提交处方审核", prescriptionReviewSubmission.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is PrescriptionReviewBoardEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("药房库存", "处方审核 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is PrescriptionReviewBoardEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("药房库存", "处方审核 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
