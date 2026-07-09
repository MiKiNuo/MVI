using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>表示 Godot 事件绑定组合示例的记录型中介者。</summary>
public sealed class EventBindingRecordingMediator : IMviMediator
{
    private IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect>? _workbenchStore;

    /// <summary>获取已记录的请求。</summary>
    public List<EventBindingWorkbenchInteractionRequest> RecordedRequests { get; } = new();

    /// <summary>设置父组合 Store。</summary>
    public void SetWorkbenchStore(IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect> workbenchStore)
    {
        _workbenchStore = workbenchStore ?? throw new ArgumentNullException(nameof(workbenchStore));
    }

    /// <summary>
    /// 发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>响应对象。</returns>
    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
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

/// <summary>表示 Godot 事件绑定组合根 ViewModel。</summary>
/// <para>
/// 3 个子组件 ViewModel（Search / Selection / Detail）由 <see cref="IEventBindingPanelFactory"/>
/// 工厂在构造期间静态注入并缓存；本 VM 仅持工厂引用，<b>不直接持有任何子 VM 引用</b>
/// （避免"VM-in-VM"反模式）。View 端通过 <see cref="CreateSearchViewModel"/>、
/// <see cref="CreateSelectionViewModel"/>、<see cref="CreateDetailViewModel"/> 三个工厂方法
/// 按需解析子 VM，再交由 ViewRegistry 创建对应 View。
/// </para>
public sealed partial class EventBindingWorkbenchViewModel
    : MviViewModelBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect>
{
    private readonly IEventBindingPanelFactory _panelFactory;

    /// <summary>初始化 Godot 事件绑定组合根 ViewModel。</summary>
    /// <param name="store">组合根状态存储。</param>
    /// <param name="panelFactory">3 个子组件 ViewModel 的工厂（搜索 / 选择 / 详情）。</param>
    public EventBindingWorkbenchViewModel(
        IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect> store,
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

/// <summary>表示 Godot 事件绑定组合示例装配结果。</summary>
public sealed class EventBindingWorkbenchComposition : IDisposable
{
    private bool _disposed;

    private EventBindingWorkbenchComposition(
        EventBindingRecordingMediator mediator,
        MviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> searchStore,
        MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> selectionStore,
        MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> detailStore,
        MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect> workbenchStore,
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
    public MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect> WorkbenchStore { get; }

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
            new EventBindingSearchIntentHandler(),
            new EventBindingSearchReducer(),
            new EventBindingSearchEffectDispatcher(mediator));
        MviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> selectionStore = new(
            EventBindingSelectionState.Initial,
            new EventBindingSelectionIntentHandler(),
            new EventBindingSelectionReducer(),
            new EventBindingSelectionEffectDispatcher(mediator));
        MviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> detailStore = new(
            EventBindingDetailState.Initial,
            new EventBindingDetailIntentHandler(),
            new EventBindingDetailReducer(),
            new EventBindingDetailEffectDispatcher(mediator));

        EventBindingSearchViewModel searchViewModel = new(searchStore);
        EventBindingSelectionViewModel selectionViewModel = new(selectionStore);
        EventBindingDetailViewModel detailViewModel = new(detailStore);
        MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect> workbenchStore = new(
            new EventBindingWorkbenchState(
                "等待 Godot 子组件事件。",
                0),
            new EventBindingWorkbenchIntentHandler(),
            new EventBindingWorkbenchReducer(),
            NullEffectDispatcher.Instance);
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

    /// <summary>
    /// 释放组合示例资源。
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
}
