using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板意图。
/// </summary>
public abstract partial record EventBindingSearchIntent : IMviIntent
{
    /// <summary>
    /// 表示查询文本变化意图。
    /// </summary>
    /// <param name="Payload">文本变化载荷。</param>
    public sealed partial record ChangeQuery(MviTextChangedEventPayload Payload) : EventBindingSearchIntent;
}

/// <summary>
/// 表示事件绑定搜索面板副作用。
/// </summary>
public abstract partial record EventBindingSearchEffect : IMviEffect
{
    /// <summary>
    /// 表示通知查询变化副作用。
    /// </summary>
    /// <param name="QueryText">查询文本。</param>
    public sealed partial record NotifyQueryChanged(string QueryText) : EventBindingSearchEffect;
}

/// <summary>
/// 表示事件绑定搜索面板副作用分发器。
/// </summary>
public sealed class EventBindingSearchEffectDispatcher : MviEffectDispatcherBase<EventBindingSearchEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定搜索面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingSearchEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        _mediator = mediator;
    }

    /// <summary>
    /// 分发具体副作用。
    /// </summary>
    /// <param name="effect">副作用（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(EventBindingSearchEffect effect, CancellationToken cancellationToken)
    {
        if (effect is EventBindingSearchEffect.NotifyQueryChanged queryChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SearchPanel", "TextChanged", queryChanged.QueryText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
