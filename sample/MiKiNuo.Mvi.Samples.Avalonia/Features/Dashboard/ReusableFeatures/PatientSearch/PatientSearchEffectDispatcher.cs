using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 副作用分发器。
/// </summary>
public sealed class PatientSearchEffectDispatcher : MviEffectDispatcherBase<PatientSearchEffect>
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

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(PatientSearchEffect effect, CancellationToken cancellationToken)
    {
        if (effect is PatientSearchEffect.RequestPatientContext patientContext)
        {
            await _mediator.SendComponentInteractionAsync(
                    patientContext.PageKey,
                    $"{patientContext.PageKey}/患者检索 MVI",
                    "选择患者上下文",
                    $"患者={patientContext.PatientName}；患者号={patientContext.PatientNo}",
                cancellationToken).ConfigureAwait(false);
        }
    }
}
