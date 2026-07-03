using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列副作用分发器。
/// </summary>
public sealed class PatientQueueEffectDispatcher : MviEffectDispatcherBase<PatientQueueEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化门诊队列副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public PatientQueueEffectDispatcher(IMviMediator mediator)
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
    protected override async ValueTask DispatchCoreAsync(PatientQueueEffect effect, CancellationToken cancellationToken)
    {
        if (effect is PatientQueueEffect.PatientSelected selected)
        {
            await _mediator.SendAsync<OpenPatientEncounterRequest, PatientEncounterResponse>(
                new OpenPatientEncounterRequest(selected.PatientName),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
