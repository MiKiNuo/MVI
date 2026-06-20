using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板状态。
/// </summary>
/// <param name="QueryText">查询文本。</param>
/// <param name="PreviousQueryText">上一次查询文本。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSearchState(
    string QueryText,
    string PreviousQueryText,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventBindingSearchState Initial { get; } = new(
        string.Empty,
        string.Empty,
        0,
        "等待 TextChanged 事件。");
}

/// <summary>
/// 表示事件绑定搜索面板意图。
/// </summary>
public abstract partial record EventBindingSearchIntent : IMviIntent
{
    /// <summary>
    /// 表示文本变化意图。
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
/// 表示事件绑定搜索面板规约器。
/// </summary>
public sealed partial class EventBindingSearchReducer
    : MviReducerBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 处理文本变化意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> Reduce(
        EventBindingSearchState state,
        EventBindingSearchIntent.ChangeQuery intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        EventBindingSearchState nextState = state with
        {
            QueryText = intent.Payload.Text,
            PreviousQueryText = intent.Payload.PreviousText ?? string.Empty,
            EventCount = state.EventCount + 1,
            StatusText = $"搜索文本变化：{intent.Payload.Text}"
        };

        return MviReduceResult.StateAndEffect<EventBindingSearchState, EventBindingSearchEffect>(
            nextState,
            new EventBindingSearchEffect.NotifyQueryChanged(intent.Payload.Text));
    }
}

/// <summary>
/// 表示事件绑定搜索面板副作用分发器。
/// </summary>
public sealed class EventBindingSearchEffectDispatcher : IMviEffectDispatcher<EventBindingSearchEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定搜索面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingSearchEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(EventBindingSearchEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect is EventBindingSearchEffect.NotifyQueryChanged queryChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SearchPanel", "TextChanged", queryChanged.QueryText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// 表示事件绑定搜索面板 ViewModel。
/// </summary>
public sealed partial class EventBindingSearchViewModel
    : MviViewModelBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 初始化事件绑定搜索面板 ViewModel。
    /// </summary>
    /// <param name="store">搜索面板 Store。</param>
    /// <param name="uiDispatcher">UI 调度器（可选）。</param>
    public EventBindingSearchViewModel(
        IMviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取查询文本。
    /// </summary>
    [MviBind(nameof(EventBindingSearchState.QueryText))]
    public partial string QueryText { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(EventBindingSearchState.StatusText))]
    public partial string StatusText { get; private set; }
}
