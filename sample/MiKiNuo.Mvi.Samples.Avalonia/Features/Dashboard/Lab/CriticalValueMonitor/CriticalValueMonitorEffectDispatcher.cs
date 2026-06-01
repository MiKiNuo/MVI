using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.CriticalValueMonitor;

/// <summary>
/// 表示危急值闭环 MVI副作用分发器。
/// </summary>
public sealed class CriticalValueMonitorEffectDispatcher : IMviEffectDispatcher<CriticalValueMonitorEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化危急值闭环 MVI副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public CriticalValueMonitorEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(CriticalValueMonitorEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is CriticalValueMonitorEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("检验医嘱", "危急值闭环 MVI", DashboardComponentActionKeys.Primary, primaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is CriticalValueMonitorEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await _mediator.SendComponentInteractionAsync("检验医嘱", "危急值闭环 MVI", DashboardComponentActionKeys.Secondary, secondaryWorkflow.ContextText,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
