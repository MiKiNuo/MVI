using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合示例的记录型中介者。
/// <para>
/// 构造时一次性注入父组合 Store，消除两阶段初始化时序耦合。
/// </para>
/// </summary>
public sealed class EventBindingRecordingMediator : IMviMediator
{
    private readonly IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> _workbenchStore;

    /// <summary>
    /// 初始化记录型中介者。
    /// </summary>
    /// <param name="workbenchStore">父组合 Store。</param>
    public EventBindingRecordingMediator(
        IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore)
    {
        _workbenchStore = workbenchStore ?? throw new ArgumentNullException(nameof(workbenchStore));
    }

    /// <summary>
    /// 获取已记录的请求。
    /// </summary>
    public List<EventBindingWorkbenchInteractionRequest> RecordedRequests { get; } = [];

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
        if (request is not EventBindingWorkbenchInteractionRequest interactionRequest)
        {
            throw new InvalidOperationException(
                $"中介者不支持请求类型：{typeof(TRequest).FullName}");
        }

        RecordedRequests.Add(interactionRequest);
        await _workbenchStore.DispatchAsync(
            new EventBindingWorkbenchIntent.RecordInteraction(
                interactionRequest.SourceComponent,
                interactionRequest.ActionKey,
                interactionRequest.ContextText),
            cancellationToken).ConfigureAwait(false);

        EventBindingWorkbenchInteractionResponse response = new(
            $"{interactionRequest.SourceComponent}:{interactionRequest.ActionKey}",
            true);

        if (response is not TResponse typedResponse)
        {
            throw new InvalidOperationException(
                $"中介者无法将响应转换为请求类型：{typeof(TResponse).FullName}");
        }

        return typedResponse;
    }
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
    /// <para>
    /// 装配顺序：先创建父组合 Store（无中介者依赖），再以 Store 注入中介者，
    /// 最后创建依赖中介者的子 Store，消除两阶段初始化。
    /// </para>
    /// </summary>
    /// <returns>组合示例。</returns>
    public static EventBindingWorkbenchComposition Create()
    {
        MviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> workbenchStore = new(
            new EventBindingWorkbenchState("等待子组件事件。", 0),
            new EventBindingWorkbenchIntentHandler(),
            new EventBindingWorkbenchReducer(),
            new EventBindingWorkbenchEffectDispatcher());

        EventBindingRecordingMediator mediator = new(workbenchStore);

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

        IEventBindingPanelFactory panelFactory = new EventBindingPanelFactory(
            searchViewModel,
            selectionViewModel,
            detailViewModel);
        EventBindingWorkbenchViewModel workbenchViewModel = new(workbenchStore, panelFactory);

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

    /// <summary>
    /// 异步释放资源。
    /// </summary>
    /// <returns>表示异步释放过程的任务。</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
