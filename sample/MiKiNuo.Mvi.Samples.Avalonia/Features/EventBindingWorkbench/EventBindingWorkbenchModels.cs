using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合示例内的组件交互请求。
/// </summary>
/// <param name="SourceComponent">来源组件。</param>
/// <param name="ActionKey">动作键。</param>
/// <param name="ContextText">上下文文本。</param>
public sealed record EventBindingWorkbenchInteractionRequest(
    string SourceComponent,
    string ActionKey,
    string ContextText);

/// <summary>
/// 表示事件绑定组合示例内的组件交互响应。
/// </summary>
/// <param name="Message">响应消息。</param>
/// <param name="Changed">是否产生变化。</param>
public sealed record EventBindingWorkbenchInteractionResponse(string Message, bool Changed);

/// <summary>
/// 表示事件绑定组合示例的记录型中介者。
/// </summary>
public sealed class EventBindingRecordingMediator : IMviMediator
{
    private IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>? _workbenchStore;

    /// <summary>
    /// 获取已记录的请求。
    /// </summary>
    public List<EventBindingWorkbenchInteractionRequest> RecordedRequests { get; } = [];

    /// <summary>
    /// 设置父组合 Store。
    /// </summary>
    /// <param name="workbenchStore">父组合 Store。</param>
    public void SetWorkbenchStore(IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore)
    {
        ArgumentNullException.ThrowIfNull(workbenchStore);
        _workbenchStore = workbenchStore;
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        if (request is not EventBindingWorkbenchInteractionRequest interactionRequest)
        {
            throw new InvalidOperationException($"事件绑定示例中介者不支持请求类型：{typeof(TRequest).FullName}");
        }

        RecordedRequests.Add(interactionRequest);
        if (_workbenchStore is not null)
        {
            await _workbenchStore.DispatchAsync(
                new EventBindingWorkbenchIntent.RecordInteraction(
                    interactionRequest.SourceComponent,
                    interactionRequest.ActionKey,
                    interactionRequest.ContextText),
                cancellationToken).ConfigureAwait(false);
        }

        object response = new EventBindingWorkbenchInteractionResponse(
            $"{interactionRequest.SourceComponent}:{interactionRequest.ActionKey}",
            true);
        return (TResponse)response;
    }
}

/// <summary>
/// 表示事件绑定组合根状态。
/// </summary>
/// <param name="SearchViewModel">搜索面板 ViewModel。</param>
/// <param name="SelectionViewModel">选择面板 ViewModel。</param>
/// <param name="DetailViewModel">详情面板 ViewModel。</param>
/// <param name="LastInteractionText">最后一次交互文本。</param>
/// <param name="InteractionCount">交互次数。</param>
public sealed record EventBindingWorkbenchState(
    EventBindingSearchViewModel SearchViewModel,
    EventBindingSelectionViewModel SelectionViewModel,
    EventBindingDetailViewModel DetailViewModel,
    string LastInteractionText,
    int InteractionCount) : IMviState;

/// <summary>
/// 表示事件绑定组合根意图。
/// </summary>
public abstract partial record EventBindingWorkbenchIntent : IMviIntent
{
    /// <summary>
    /// 表示记录子组件交互意图。
    /// </summary>
    /// <param name="SourceComponent">来源组件。</param>
    /// <param name="ActionKey">动作键。</param>
    /// <param name="ContextText">上下文文本。</param>
    public sealed partial record RecordInteraction(
        string SourceComponent,
        string ActionKey,
        string ContextText) : EventBindingWorkbenchIntent;
}

/// <summary>
/// 表示事件绑定组合根副作用。
/// </summary>
public abstract partial record EventBindingWorkbenchEffect : IMviEffect;

/// <summary>
/// 表示事件绑定组合根规约器。
/// </summary>
public sealed partial class EventBindingWorkbenchReducer
    : MviReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 处理记录子组件交互意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> Reduce(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent.RecordInteraction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state with
        {
            LastInteractionText = $"{intent.SourceComponent}/{intent.ActionKey}: {intent.ContextText}",
            InteractionCount = state.InteractionCount + 1
        });
    }
}

/// <summary>
/// 表示事件绑定组合根空副作用分发器。
/// </summary>
public sealed class EventBindingWorkbenchEffectDispatcher : IMviEffectDispatcher<EventBindingWorkbenchEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(EventBindingWorkbenchEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 表示事件绑定组合根 ViewModel。
/// </summary>
public sealed partial class EventBindingWorkbenchViewModel
    : MviViewModelBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 初始化事件绑定组合根 ViewModel。
    /// </summary>
    /// <param name="store">组合根 Store。</param>
    public EventBindingWorkbenchViewModel(IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取搜索面板 ViewModel。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.SearchViewModel))]
    public partial EventBindingSearchViewModel SearchViewModel { get; private set; }

    /// <summary>
    /// 获取选择面板 ViewModel。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.SelectionViewModel))]
    public partial EventBindingSelectionViewModel SelectionViewModel { get; private set; }

    /// <summary>
    /// 获取详情面板 ViewModel。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.DetailViewModel))]
    public partial EventBindingDetailViewModel DetailViewModel { get; private set; }

    /// <summary>
    /// 获取最后一次交互文本。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.LastInteractionText))]
    public partial string LastInteractionText { get; private set; }

    /// <summary>
    /// 获取交互次数。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.InteractionCount))]
    public partial int InteractionCount { get; private set; }
}

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
    public EventBindingSearchViewModel(IMviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
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

    /// <summary>
    /// 获取文本变化命令。
    /// </summary>
    [MviCommand(typeof(EventBindingSearchIntent.ChangeQuery))]
    public partial IMviCommand QueryTextChangedCommand { get; private set; }
}

/// <summary>
/// 表示事件绑定选择面板状态。
/// </summary>
/// <param name="PatientIds">可选患者编号。</param>
/// <param name="SelectedPatientId">选中患者编号。</param>
/// <param name="SelectedIndex">选中索引。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSelectionState(
    IReadOnlyList<string> PatientIds,
    string SelectedPatientId,
    int SelectedIndex,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventBindingSelectionState Initial { get; } = new(
        new[] { "P10001", "P10002", "P10003" },
        "-",
        -1,
        0,
        "等待 SelectionChanged 事件。");
}

/// <summary>
/// 表示事件绑定选择面板意图。
/// </summary>
public abstract partial record EventBindingSelectionIntent : IMviIntent
{
    /// <summary>
    /// 表示选择变化意图。
    /// </summary>
    /// <param name="Payload">选择变化载荷。</param>
    public sealed partial record ChangeSelection(MviSelectionChangedEventPayload Payload) : EventBindingSelectionIntent;
}

/// <summary>
/// 表示事件绑定选择面板副作用。
/// </summary>
public abstract partial record EventBindingSelectionEffect : IMviEffect
{
    /// <summary>
    /// 表示通知选择变化副作用。
    /// </summary>
    /// <param name="PatientId">患者编号。</param>
    public sealed partial record NotifySelectionChanged(string PatientId) : EventBindingSelectionEffect;
}

/// <summary>
/// 表示事件绑定选择面板规约器。
/// </summary>
public sealed partial class EventBindingSelectionReducer
    : MviReducerBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 处理选择变化意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> Reduce(
        EventBindingSelectionState state,
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        string patientId = intent.Payload.SelectedValue?.ToString() ?? "-";
        EventBindingSelectionState nextState = state with
        {
            SelectedPatientId = patientId,
            SelectedIndex = intent.Payload.SelectedIndex ?? -1,
            EventCount = state.EventCount + 1,
            StatusText = $"选择患者：{patientId}"
        };

        return MviReduceResult.StateAndEffect<EventBindingSelectionState, EventBindingSelectionEffect>(
            nextState,
            new EventBindingSelectionEffect.NotifySelectionChanged(patientId));
    }
}

/// <summary>
/// 表示事件绑定选择面板副作用分发器。
/// </summary>
public sealed class EventBindingSelectionEffectDispatcher : IMviEffectDispatcher<EventBindingSelectionEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定选择面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingSelectionEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(EventBindingSelectionEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect is EventBindingSelectionEffect.NotifySelectionChanged selectionChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SelectionPanel", "SelectionChanged", selectionChanged.PatientId),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// 表示事件绑定选择面板 ViewModel。
/// </summary>
public sealed partial class EventBindingSelectionViewModel
    : MviViewModelBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 初始化事件绑定选择面板 ViewModel。
    /// </summary>
    /// <param name="store">选择面板 Store。</param>
    public EventBindingSelectionViewModel(IMviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取可选患者编号。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.PatientIds))]
    public partial IReadOnlyList<string> PatientIds { get; private set; }

    /// <summary>
    /// 获取选中患者编号。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.SelectedPatientId))]
    public partial string SelectedPatientId { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取选择变化命令。
    /// </summary>
    [MviCommand(typeof(EventBindingSelectionIntent.ChangeSelection))]
    public partial IMviCommand SelectionChangedCommand { get; private set; }
}

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
/// 表示事件绑定详情面板规约器。
/// </summary>
public sealed partial class EventBindingDetailReducer
    : MviReducerBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>
    /// 处理详情指针按下意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> Reduce(
        EventBindingDetailState state,
        EventBindingDetailIntent.PressDetail intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        string pointerText = $"Pointer {intent.Payload.Button} @ {intent.Payload.PositionX:0},{intent.Payload.PositionY:0}";
        EventBindingDetailState nextState = state with
        {
            LastPointerText = pointerText,
            StatusText = "详情区域收到 PointerPressed。"
        };

        return MviReduceResult.StateAndEffect<EventBindingDetailState, EventBindingDetailEffect>(
            nextState,
            new EventBindingDetailEffect.NotifyDetailEvent("PointerPressed", pointerText));
    }

    /// <summary>
    /// 处理刷新动作意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> Reduce(
        EventBindingDetailState state,
        EventBindingDetailIntent.Refresh intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        EventBindingDetailState nextState = state with
        {
            RefreshCount = state.RefreshCount + 1,
            StatusText = $"刷新动作：{intent.Payload.SourceName}"
        };

        return MviReduceResult.StateAndEffect<EventBindingDetailState, EventBindingDetailEffect>(
            nextState,
            new EventBindingDetailEffect.NotifyDetailEvent("Action", intent.Payload.SourceName ?? "Unknown"));
    }
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

    /// <inheritdoc />
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
    public EventBindingDetailViewModel(IMviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
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

    /// <summary>
    /// 获取详情区域指针命令。
    /// </summary>
    [MviCommand(typeof(EventBindingDetailIntent.PressDetail))]
    public partial IMviCommand DetailPressedCommand { get; private set; }

    /// <summary>
    /// 获取刷新命令。
    /// </summary>
    [MviCommand(typeof(EventBindingDetailIntent.Refresh))]
    public partial IMviCommand RefreshCommand { get; private set; }
}

/// <summary>
/// 表示事件绑定组合示例装配结果。
/// </summary>
public sealed class EventBindingWorkbenchComposition : IAsyncDisposable, IDisposable
{
    private bool _disposed;

    private EventBindingWorkbenchComposition(
        EventBindingRecordingMediator mediator,
        MviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> searchStore,
        MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> selectionStore,
        MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> detailStore,
        MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore,
        EventBindingSearchViewModel searchViewModel,
        EventBindingSelectionViewModel selectionViewModel,
        EventBindingDetailViewModel detailViewModel,
        EventBindingWorkbenchViewModel workbenchViewModel)
    {
        Mediator = mediator;
        SearchStore = searchStore;
        SelectionStore = selectionStore;
        DetailStore = detailStore;
        WorkbenchStore = workbenchStore;
        SearchViewModel = searchViewModel;
        SelectionViewModel = selectionViewModel;
        DetailViewModel = detailViewModel;
        WorkbenchViewModel = workbenchViewModel;
    }

    /// <summary>
    /// 获取记录型中介者。
    /// </summary>
    public EventBindingRecordingMediator Mediator { get; }

    /// <summary>
    /// 获取搜索面板 Store。
    /// </summary>
    public MviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> SearchStore { get; }

    /// <summary>
    /// 获取选择面板 Store。
    /// </summary>
    public MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> SelectionStore { get; }

    /// <summary>
    /// 获取详情面板 Store。
    /// </summary>
    public MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> DetailStore { get; }

    /// <summary>
    /// 获取组合根 Store。
    /// </summary>
    public MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> WorkbenchStore { get; }

    /// <summary>
    /// 获取搜索面板 ViewModel。
    /// </summary>
    public EventBindingSearchViewModel SearchViewModel { get; }

    /// <summary>
    /// 获取选择面板 ViewModel。
    /// </summary>
    public EventBindingSelectionViewModel SelectionViewModel { get; }

    /// <summary>
    /// 获取详情面板 ViewModel。
    /// </summary>
    public EventBindingDetailViewModel DetailViewModel { get; }

    /// <summary>
    /// 获取组合根 ViewModel。
    /// </summary>
    public EventBindingWorkbenchViewModel WorkbenchViewModel { get; }

    /// <summary>
    /// 创建事件绑定组合示例。
    /// </summary>
    /// <returns>组合示例。</returns>
    public static EventBindingWorkbenchComposition Create()
    {
        EventBindingRecordingMediator mediator = new();
        MviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> searchStore = new(
            EventBindingSearchState.Initial,
            new EventBindingSearchReducer(),
            new EventBindingSearchEffectDispatcher(mediator));
        MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> selectionStore = new(
            EventBindingSelectionState.Initial,
            new EventBindingSelectionReducer(),
            new EventBindingSelectionEffectDispatcher(mediator));
        MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> detailStore = new(
            EventBindingDetailState.Initial,
            new EventBindingDetailReducer(),
            new EventBindingDetailEffectDispatcher(mediator));

        EventBindingSearchViewModel searchViewModel = new(searchStore);
        EventBindingSelectionViewModel selectionViewModel = new(selectionStore);
        EventBindingDetailViewModel detailViewModel = new(detailStore);

        MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore = new(
            new EventBindingWorkbenchState(
                searchViewModel,
                selectionViewModel,
                detailViewModel,
                "等待子组件事件。",
                0),
            new EventBindingWorkbenchReducer(),
            new EventBindingWorkbenchEffectDispatcher());
        mediator.SetWorkbenchStore(workbenchStore);
        EventBindingWorkbenchViewModel workbenchViewModel = new(workbenchStore);

        return new EventBindingWorkbenchComposition(
            mediator,
            searchStore,
            selectionStore,
            detailStore,
            workbenchStore,
            searchViewModel,
            selectionViewModel,
            detailViewModel,
            workbenchViewModel);
    }

    /// <summary>
    /// 释放组合示例。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        WorkbenchViewModel.Dispose();
        DetailViewModel.Dispose();
        SelectionViewModel.Dispose();
        SearchViewModel.Dispose();
        WorkbenchStore.Dispose();
        DetailStore.Dispose();
        SelectionStore.Dispose();
        SearchStore.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
