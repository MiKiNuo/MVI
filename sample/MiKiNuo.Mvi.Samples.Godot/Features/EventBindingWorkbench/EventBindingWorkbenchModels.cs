using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>表示 Godot 事件绑定组合示例内的组件交互请求。</summary>
/// <param name="SourceComponent">来源组件。</param>
/// <param name="ActionKey">动作键。</param>
/// <param name="ContextText">上下文文本。</param>
public sealed record EventBindingWorkbenchInteractionRequest(
    string SourceComponent,
    string ActionKey,
    string ContextText);

/// <summary>表示 Godot 事件绑定组合示例内的组件交互响应。</summary>
/// <param name="Message">响应消息。</param>
/// <param name="Changed">是否产生变化。</param>
public sealed record EventBindingWorkbenchInteractionResponse(string Message, bool Changed);

/// <summary>表示 Godot 事件绑定组合示例的记录型中介者。</summary>
public sealed class EventBindingRecordingMediator : IMviMediator
{
    private IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>? _workbenchStore;

    /// <summary>获取已记录的请求。</summary>
    public List<EventBindingWorkbenchInteractionRequest> RecordedRequests { get; } = new();

    /// <summary>设置父组合 Store。</summary>
    public void SetWorkbenchStore(IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore)
    {
        _workbenchStore = workbenchStore ?? throw new ArgumentNullException(nameof(workbenchStore));
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        if (request is not EventBindingWorkbenchInteractionRequest interactionRequest)
        {
            throw new InvalidOperationException($"Godot 事件绑定示例中介者不支持请求类型：{typeof(TRequest).FullName}");
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

/// <summary>表示 Godot 事件绑定组合根状态。</summary>
/// <param name="LastInteractionText">最后一次交互文本。</param>
/// <param name="InteractionCount">交互次数。</param>
public sealed record EventBindingWorkbenchState(
    string LastInteractionText,
    int InteractionCount) : IMviState;

/// <summary>表示 Godot 事件绑定组合根意图。</summary>
public abstract partial record EventBindingWorkbenchIntent : IMviIntent
{
    /// <summary>表示记录子组件交互意图。</summary>
    /// <param name="SourceComponent">来源组件。</param>
    /// <param name="ActionKey">动作键。</param>
    /// <param name="ContextText">上下文文本。</param>
    public sealed partial record RecordInteraction(
        string SourceComponent,
        string ActionKey,
        string ContextText) : EventBindingWorkbenchIntent;
}

/// <summary>表示 Godot 事件绑定组合根副作用。</summary>
public abstract partial record EventBindingWorkbenchEffect : IMviEffect;

/// <summary>表示 Godot 事件绑定组合根规约器。</summary>
public sealed partial class EventBindingWorkbenchReducer
    : MviReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>处理记录子组件交互意图。</summary>
    [MviReduce]
    private MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> Reduce(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent.RecordInteraction intent)
    {
        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state with
        {
            LastInteractionText = $"{intent.SourceComponent}/{intent.ActionKey}: {intent.ContextText}",
            InteractionCount = state.InteractionCount + 1
        });
    }
}

/// <summary>表示 Godot 事件绑定组合根空副作用分发器。</summary>
public sealed class EventBindingWorkbenchEffectDispatcher : IMviEffectDispatcher<EventBindingWorkbenchEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(EventBindingWorkbenchEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>表示 Godot 事件绑定组合根 ViewModel。</summary>
/// <para>
/// 3 个子组件 ViewModel（Search / Selection / Detail）由 <see cref="IEventBindingPanelFactory"/>
/// 工厂在构造期间静态注入并缓存；本 VM 仅持工厂引用，<b>不直接持有任何子 VM 引用</b>
/// （避免"VM-in-VM"反模式）。View 端通过 <see cref="CreateSearchViewModel"/>、
/// <see cref="CreateSelectionViewModel"/>、<see cref="CreateDetailViewModel"/> 三个工厂方法
/// 按需解析子 VM，再交由 ViewRegistry 创建对应 View。
/// </para>
public sealed partial class EventBindingWorkbenchViewModel
    : MviViewModelBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    private readonly IEventBindingPanelFactory _panelFactory;

    /// <summary>初始化 Godot 事件绑定组合根 ViewModel。</summary>
    /// <param name="store">组合根状态存储。</param>
    /// <param name="panelFactory">3 个子组件 ViewModel 的工厂（搜索 / 选择 / 详情）。</param>
    public EventBindingWorkbenchViewModel(
        IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> store,
        IEventBindingPanelFactory panelFactory)
        : base(store)
    {
        ArgumentNullException.ThrowIfNull(panelFactory);
        _panelFactory = panelFactory;
    }

    /// <summary>解析搜索面板子组件 ViewModel（经由 <see cref="IEventBindingPanelFactory"/> 工厂缓存返回）。</summary>
    /// <returns>搜索 <c>EventBindingSearchViewModel</c> 实例。</returns>
    public object CreateSearchViewModel() => _panelFactory.CreateSearchViewModel();

    /// <summary>解析选择面板子组件 ViewModel（经由 <see cref="IEventBindingPanelFactory"/> 工厂缓存返回）。</summary>
    /// <returns>选择 <c>EventBindingSelectionViewModel</c> 实例。</returns>
    public object CreateSelectionViewModel() => _panelFactory.CreateSelectionViewModel();

    /// <summary>解析详情面板子组件 ViewModel（经由 <see cref="IEventBindingPanelFactory"/> 工厂缓存返回）。</summary>
    /// <returns>详情 <c>EventBindingDetailViewModel</c> 实例。</returns>
    public object CreateDetailViewModel() => _panelFactory.CreateDetailViewModel();

    /// <summary>获取最后一次交互文本。</summary>
    [MviBind(nameof(EventBindingWorkbenchState.LastInteractionText))]
    public partial string LastInteractionText { get; private set; }

    /// <summary>获取交互次数。</summary>
    [MviBind(nameof(EventBindingWorkbenchState.InteractionCount))]
    public partial int InteractionCount { get; private set; }
}

/// <summary>表示 Godot 搜索面板状态。</summary>
/// <param name="QueryText">查询文本。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSearchState(
    string QueryText,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingSearchState Initial { get; } = new(string.Empty, 0, "等待 LineEdit.TextChanged。");
}

/// <summary>表示 Godot 搜索面板意图。</summary>
public abstract partial record EventBindingSearchIntent : IMviIntent
{
    /// <summary>表示查询文本变化意图。</summary>
    /// <param name="Payload">文本变化载荷。</param>
    public sealed partial record ChangeQuery(MviTextChangedEventPayload Payload) : EventBindingSearchIntent;
}

/// <summary>表示 Godot 搜索面板副作用。</summary>
public abstract partial record EventBindingSearchEffect : IMviEffect
{
    /// <summary>表示通知查询变化副作用。</summary>
    /// <param name="QueryText">查询文本。</param>
    public sealed partial record NotifyQueryChanged(string QueryText) : EventBindingSearchEffect;
}

/// <summary>表示 Godot 搜索面板规约器。</summary>
public sealed partial class EventBindingSearchReducer
    : MviReducerBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>处理查询文本变化意图。</summary>
    [MviReduce]
    private MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> Reduce(
        EventBindingSearchState state,
        EventBindingSearchIntent.ChangeQuery intent)
    {
        EventBindingSearchState nextState = state with
        {
            QueryText = intent.Payload.Text,
            EventCount = state.EventCount + 1,
            StatusText = $"搜索文本变化：{intent.Payload.Text}"
        };
        return MviReduceResult.StateAndEffect<EventBindingSearchState, EventBindingSearchEffect>(
            nextState,
            new EventBindingSearchEffect.NotifyQueryChanged(intent.Payload.Text));
    }
}

/// <summary>表示 Godot 搜索面板副作用分发器。</summary>
public sealed class EventBindingSearchEffectDispatcher : IMviEffectDispatcher<EventBindingSearchEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>初始化 Godot 搜索面板副作用分发器。</summary>
    public EventBindingSearchEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(EventBindingSearchEffect effect, CancellationToken cancellationToken = default)
    {
        if (effect is EventBindingSearchEffect.NotifyQueryChanged queryChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SearchPanel", "TextChanged", queryChanged.QueryText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>表示 Godot 搜索面板 ViewModel。</summary>
public sealed partial class EventBindingSearchViewModel
    : MviViewModelBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>初始化 Godot 搜索面板 ViewModel。</summary>
    public EventBindingSearchViewModel(IMviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取查询文本。</summary>
    [MviBind(nameof(EventBindingSearchState.QueryText))]
    public partial string QueryText { get; private set; }

    /// <summary>获取查询文本变化命令。</summary>
    [MviCommand(typeof(EventBindingSearchIntent.ChangeQuery))]
    public partial IMviCommand QueryTextChangedCommand { get; private set; }
}

/// <summary>表示 Godot 选择面板状态。</summary>
/// <param name="SelectedMissionId">选中任务编号。</param>
/// <param name="SelectedIndex">选中索引。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSelectionState(
    string SelectedMissionId,
    int SelectedIndex,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingSelectionState Initial { get; } = new("-", -1, 0, "等待 ItemList.ItemSelected。");
}

/// <summary>表示 Godot 选择面板意图。</summary>
public abstract partial record EventBindingSelectionIntent : IMviIntent
{
    /// <summary>表示选择变化意图。</summary>
    /// <param name="Payload">选择变化载荷。</param>
    public sealed partial record ChangeSelection(MviSelectionChangedEventPayload Payload) : EventBindingSelectionIntent;
}

/// <summary>表示 Godot 选择面板副作用。</summary>
public abstract partial record EventBindingSelectionEffect : IMviEffect
{
    /// <summary>表示通知选择变化副作用。</summary>
    /// <param name="MissionId">任务编号。</param>
    public sealed partial record NotifySelectionChanged(string MissionId) : EventBindingSelectionEffect;
}

/// <summary>表示 Godot 选择面板规约器。</summary>
public sealed partial class EventBindingSelectionReducer
    : MviReducerBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>处理选择变化意图。</summary>
    [MviReduce]
    private MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> Reduce(
        EventBindingSelectionState state,
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        string missionId = intent.Payload.SelectedValue?.ToString() ?? "-";
        EventBindingSelectionState nextState = state with
        {
            SelectedMissionId = missionId,
            SelectedIndex = intent.Payload.SelectedIndex ?? -1,
            EventCount = state.EventCount + 1,
            StatusText = $"选择任务：{missionId}"
        };
        return MviReduceResult.StateAndEffect<EventBindingSelectionState, EventBindingSelectionEffect>(
            nextState,
            new EventBindingSelectionEffect.NotifySelectionChanged(missionId));
    }
}

/// <summary>表示 Godot 选择面板副作用分发器。</summary>
public sealed class EventBindingSelectionEffectDispatcher : IMviEffectDispatcher<EventBindingSelectionEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>初始化 Godot 选择面板副作用分发器。</summary>
    public EventBindingSelectionEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(EventBindingSelectionEffect effect, CancellationToken cancellationToken = default)
    {
        if (effect is EventBindingSelectionEffect.NotifySelectionChanged selectionChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SelectionPanel", "ItemSelected", selectionChanged.MissionId),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>表示 Godot 选择面板 ViewModel。</summary>
public sealed partial class EventBindingSelectionViewModel
    : MviViewModelBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>初始化 Godot 选择面板 ViewModel。</summary>
    public EventBindingSelectionViewModel(IMviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取选中任务编号。</summary>
    [MviBind(nameof(EventBindingSelectionState.SelectedMissionId))]
    public partial string SelectedMissionId { get; private set; }

    /// <summary>获取选择变化命令。</summary>
    [MviCommand(typeof(EventBindingSelectionIntent.ChangeSelection))]
    public partial IMviCommand SelectionChangedCommand { get; private set; }
}

/// <summary>表示 Godot 详情面板状态。</summary>
/// <param name="PrepareCount">准备次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingDetailState(int PrepareCount, string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingDetailState Initial { get; } = new(0, "等待 Button.Pressed。");
}

/// <summary>表示 Godot 详情面板意图。</summary>
public abstract partial record EventBindingDetailIntent : IMviIntent
{
    /// <summary>表示准备动作意图。</summary>
    /// <param name="Payload">动作事件载荷。</param>
    public sealed partial record Prepare(MviActionEventPayload Payload) : EventBindingDetailIntent;
}

/// <summary>表示 Godot 详情面板副作用。</summary>
public abstract partial record EventBindingDetailEffect : IMviEffect
{
    /// <summary>表示通知准备动作副作用。</summary>
    /// <param name="SourceName">事件来源名称。</param>
    public sealed partial record NotifyPrepare(string SourceName) : EventBindingDetailEffect;
}

/// <summary>表示 Godot 详情面板规约器。</summary>
public sealed partial class EventBindingDetailReducer
    : MviReducerBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>处理准备动作意图。</summary>
    [MviReduce]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> Reduce(
        EventBindingDetailState state,
        EventBindingDetailIntent.Prepare intent)
    {
        string sourceName = intent.Payload.SourceName ?? "Unknown";
        EventBindingDetailState nextState = state with
        {
            PrepareCount = state.PrepareCount + 1,
            StatusText = $"准备动作：{sourceName}"
        };
        return MviReduceResult.StateAndEffect<EventBindingDetailState, EventBindingDetailEffect>(
            nextState,
            new EventBindingDetailEffect.NotifyPrepare(sourceName));
    }
}

/// <summary>表示 Godot 详情面板副作用分发器。</summary>
public sealed class EventBindingDetailEffectDispatcher : IMviEffectDispatcher<EventBindingDetailEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>初始化 Godot 详情面板副作用分发器。</summary>
    public EventBindingDetailEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(EventBindingDetailEffect effect, CancellationToken cancellationToken = default)
    {
        if (effect is EventBindingDetailEffect.NotifyPrepare prepare)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("DetailPanel", "Pressed", prepare.SourceName),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>表示 Godot 详情面板 ViewModel。</summary>
public sealed partial class EventBindingDetailViewModel
    : MviViewModelBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>初始化 Godot 详情面板 ViewModel。</summary>
    public EventBindingDetailViewModel(IMviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>获取准备次数。</summary>
    [MviBind(nameof(EventBindingDetailState.PrepareCount))]
    public partial int PrepareCount { get; private set; }

    /// <summary>获取准备命令。</summary>
    [MviCommand(typeof(EventBindingDetailIntent.Prepare))]
    public partial IMviCommand PrepareCommand { get; private set; }
}

/// <summary>表示 Godot 事件绑定组合示例装配结果。</summary>
public sealed class EventBindingWorkbenchComposition : IDisposable
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

    /// <summary>获取记录型中介者。</summary>
    public EventBindingRecordingMediator Mediator { get; }

    /// <summary>获取搜索面板 Store。</summary>
    public MviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> SearchStore { get; }

    /// <summary>获取选择面板 Store。</summary>
    public MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> SelectionStore { get; }

    /// <summary>获取详情面板 Store。</summary>
    public MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> DetailStore { get; }

    /// <summary>获取组合根 Store。</summary>
    public MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> WorkbenchStore { get; }

    /// <summary>获取搜索面板 ViewModel。</summary>
    public EventBindingSearchViewModel SearchViewModel { get; }

    /// <summary>获取选择面板 ViewModel。</summary>
    public EventBindingSelectionViewModel SelectionViewModel { get; }

    /// <summary>获取详情面板 ViewModel。</summary>
    public EventBindingDetailViewModel DetailViewModel { get; }

    /// <summary>获取组合根 ViewModel。</summary>
    public EventBindingWorkbenchViewModel WorkbenchViewModel { get; }

    /// <summary>创建 Godot 事件绑定组合示例。</summary>
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
                "等待 Godot 子组件事件。",
                0),
            new EventBindingWorkbenchReducer(),
            new EventBindingWorkbenchEffectDispatcher());
        mediator.SetWorkbenchStore(workbenchStore);
        // 父 VM 不再直接持有 3 个子 VM 引用；改用 IEventBindingPanelFactory 工厂封装 3 个子 VM，
        // 由父 VM 在 View 按需解析时通过工厂方法获取，避免"VM-in-VM"反模式。
        IEventBindingPanelFactory panelFactory = new EventBindingPanelFactory(
            searchViewModel,
            selectionViewModel,
            detailViewModel);
        EventBindingWorkbenchViewModel workbenchViewModel = new(
            workbenchStore,
            panelFactory);

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

    /// <inheritdoc />
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
}
