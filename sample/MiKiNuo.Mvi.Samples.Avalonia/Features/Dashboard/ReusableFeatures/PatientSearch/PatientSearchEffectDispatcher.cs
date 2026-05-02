using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 副作用分发器。
/// </summary>
public sealed class PatientSearchEffectDispatcher : IMviEffectDispatcher<PatientSearchEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化可复用患者检索 MVI 副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public PatientSearchEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(PatientSearchEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is PatientSearchEffect.RequestPatientContext patientContext)
        {
            await _mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
                new DashboardComponentInteractionRequest(
                    patientContext.PageKey,
                    $"{patientContext.PageKey}/患者检索 MVI",
                    "选择患者上下文",
                    $"患者={patientContext.PatientName}；患者号={patientContext.PatientNo}"),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
