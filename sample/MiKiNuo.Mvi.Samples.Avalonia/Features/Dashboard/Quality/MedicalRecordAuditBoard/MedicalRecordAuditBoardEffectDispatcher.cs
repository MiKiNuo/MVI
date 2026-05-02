using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.MedicalRecordAuditBoard;

/// <summary>
/// 表示病历质控 MVI副作用分发器。
/// </summary>
public sealed class MedicalRecordAuditBoardEffectDispatcher : IMviEffectDispatcher<MedicalRecordAuditBoardEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化病历质控 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public MedicalRecordAuditBoardEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(MedicalRecordAuditBoardEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is MedicalRecordAuditBoardEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("运营质控", "病历质控 MVI", "Primary", primaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is MedicalRecordAuditBoardEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest("运营质控", "病历质控 MVI", "Secondary", secondaryWorkflow.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
