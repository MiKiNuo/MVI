using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板状态。
/// </summary>
/// <param name="LastPointerText">最后一次指针文本。</param>
/// <param name="RefreshCount">刷新次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingDetailState(
    string LastPointerText,
    int RefreshCount,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventBindingDetailState Initial { get; } = new(
        "未触发 PointerPressed。",
        0,
        "等待详情区域事件。");
}

/// <summary>
/// 表示事件绑定详情面板意图。
/// </summary>
public abstract partial record EventBindingDetailIntent : IMviIntent
{
    /// <summary>
    /// 表示详情区域指针按下意图。
    /// </summary>
    /// <param name="Payload">指针事件载荷。</param>
    public sealed partial record PressDetail(MviPointerEventPayload Payload) : EventBindingDetailIntent;

    /// <summary>
    /// 表示刷新动作意图。
    /// </summary>
    /// <param name="Payload">动作事件载荷。</param>
    public sealed partial record Refresh(MviActionEventPayload Payload) : EventBindingDetailIntent;
}

/// <summary>
/// 表示事件绑定详情面板副作用。
/// </summary>
public abstract partial record EventBindingDetailEffect : IMviEffect
{
    /// <summary>
    /// 表示通知详情事件副作用。
    /// </summary>
    /// <param name="ActionKey">动作键。</param>
    /// <param name="ContextText">上下文文本。</param>
    public sealed partial record NotifyDetailEvent(string ActionKey, string ContextText) : EventBindingDetailEffect;
}

/// <summary>
/// 表示事件绑定详情面板副作用分发器。
/// </summary>
public sealed class EventBindingDetailEffectDispatcher : IMviEffectDispatcher<EventBindingDetailEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定详情面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingDetailEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(EventBindingDetailEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect is EventBindingDetailEffect.NotifyDetailEvent detailEvent)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("DetailPanel", detailEvent.ActionKey, detailEvent.ContextText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// 表示事件绑定详情面板 ViewModel。
/// </summary>
public sealed partial class EventBindingDetailViewModel
    : MviViewModelBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>
    /// 初始化事件绑定详情面板 ViewModel。
    /// </summary>
    /// <param name="store">详情面板 Store。</param>
    /// <param name="uiDispatcher">UI 调度器（可选）。</param>
    public EventBindingDetailViewModel(
        IMviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取最后一次指针文本。
    /// </summary>
    [MviBind(nameof(EventBindingDetailState.LastPointerText))]
    public partial string LastPointerText { get; private set; }

    /// <summary>
    /// 获取刷新次数。
    /// </summary>
    [MviBind(nameof(EventBindingDetailState.RefreshCount))]
    public partial int RefreshCount { get; private set; }
}
