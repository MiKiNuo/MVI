using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.NursingTaskBoard;

/// <summary>
/// 表示护理任务 MVI副作用分发器。
/// </summary>
public sealed class NursingTaskBoardEffectDispatcher : IMviEffectDispatcher<NursingTaskBoardEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化护理任务 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public NursingTaskBoardEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(NursingTaskBoardEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is NursingTaskBoardEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "护理任务 MVI", DashboardComponentActionKeys.Primary, primaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is NursingTaskBoardEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("住院床位", "护理任务 MVI", DashboardComponentActionKeys.Secondary, secondaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
