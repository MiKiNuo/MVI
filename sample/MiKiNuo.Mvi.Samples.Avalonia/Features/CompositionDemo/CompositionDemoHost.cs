using MiKiNuo.Mvi.Application.MVI.Composition;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;

/// <summary>三个演示功能共用的纯视图适配，只访问自己的存储。</summary>
/// <typeparam name="TState">本实例状态。</typeparam>
/// <typeparam name="TIntent">本实例意图。</typeparam>
/// <typeparam name="TEffect">本实例副作用。</typeparam>
public sealed class CompositionDemoViewModel<TState, TIntent, TEffect>(MviStore<TState, TIntent, TEffect> store)
    : MviViewModelBase<TState, TIntent, TEffect>(store)
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>通知视图重新读取本实例的状态投影。</summary>
    /// <param name="state">最新状态。</param>
    protected override void ApplyStateCore(TState state) => OnPropertyChanged(nameof(State));

    /// <summary>获取本实例当前状态供视图展示。</summary>
    public TState State => Store.CurrentState;
    /// <summary>获取本实例状态流供宿主视图绑定。</summary>
    public Observable<TState> States => Store.States;
    /// <summary>通过本实例管线执行用户意图。</summary>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消信号。</param>
    /// <returns>派发完成任务。</returns>
    public new ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
        => Store.DispatchAsync(intent, cancellationToken);
}

/// <summary>手工装配处方演示，不参与特性生成候选发现。</summary>
public sealed class PrescriptionDemoHost : IAsyncDisposable
{
    private readonly MviFeatureInstance<CompositionDemoViewModel<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect>> _search;
    private readonly MviFeatureInstance<CompositionDemoViewModel<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect>> _details;
    private readonly MviCompositionScope _scope = new();
    private readonly MviFeatureInstance<CompositionDemoViewModel<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect>> _workspace;

    /// <summary>建立相互隔离的检索和明细实例并接线。</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000", Justification = "Store 与端点所有权通过资源列表转交 MviFeatureInstance，宿主等待逆序释放。")]
    public PrescriptionDemoHost()
    {
        MviMediatorEndpoint searchEndpoint = _scope.CreateEndpoint(Guid.NewGuid());
        MviMediatorEndpoint detailsEndpoint = _scope.CreateEndpoint(Guid.NewGuid());
        MviMediatorEndpoint parentEndpoint = _scope.CreateEndpoint(Guid.NewGuid());
        MviStore<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect> searchStore = new(new(), new MedicineSearchReducer(), new MedicineSearchEffectDispatcher(searchEndpoint));
        Search = new(searchStore);
        _search = new(searchEndpoint.InstanceId, Search, [Search, searchStore, searchEndpoint]);
        MviStore<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect> detailsStore = new(new(), new MedicationDetailsReducer(), new MedicationDetailsEffectDispatcher(detailsEndpoint));
        Details = new(detailsStore);
        _details = new(detailsEndpoint.InstanceId, Details, [Details, detailsStore, detailsEndpoint]);
        _scope.Register<SubmitMedicine, MedicineSelectionResult>(detailsEndpoint.InstanceId, async (request, token) =>
        {
            await detailsStore.DispatchAsync(new MedicationDetailsIntent.Add(request.Name), token);
            return new MedicineSelectionResult(request.Name);
        });
        _scope.Bind<SubmitMedicine>(searchEndpoint.InstanceId, detailsEndpoint.InstanceId);
        MviStore<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect> parentStore = new(new(), new PrescriptionWorkspaceReducer(), new PrescriptionWorkspaceDispatcher());
        Workspace = new(parentStore);
        IDisposable summary = _scope.Subscribe<MedicineAdded>(parentEndpoint.InstanceId, change =>
        {
            if (!parentStore.TryPost(new(change))) throw new InvalidOperationException("父级已关闭或通知队列已满。");
        });
        _workspace = new(parentEndpoint.InstanceId, Workspace, [parentEndpoint, parentStore, Workspace, summary]);
        _workspace.Own(_search).AsTask().GetAwaiter().GetResult();
        _workspace.Own(_details).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>获取可展示的检索视图模型。</summary>
    public CompositionDemoViewModel<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect> Search { get; }
    /// <summary>获取可展示的明细视图模型。</summary>
    public CompositionDemoViewModel<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect> Details { get; }
    /// <summary>获取组合 MVI 自身的视图模型。</summary>
    public CompositionDemoViewModel<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect> Workspace { get; }

    /// <summary>释放完整处方对象图。</summary>
    /// <returns>释放任务。</returns>
    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _workspace.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>只装配检索功能的独立宿主，复用与处方组合相同的业务实现。</summary>
public sealed class MedicineSearchDemoHost : IAsyncDisposable
{
    private readonly MviCompositionScope _scope = new();
    private readonly MviFeatureInstance<CompositionDemoViewModel<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect>> _instance;

    /// <summary>把选择契约接到独立宿主的接收服务。</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000", Justification = "资源交给实例统一释放。")]
    public MedicineSearchDemoHost()
    {
        MviMediatorEndpoint endpoint = _scope.CreateEndpoint(Guid.NewGuid());
        MviMediatorEndpoint receiver = _scope.CreateEndpoint(Guid.NewGuid());
        _scope.Register<SubmitMedicine, MedicineSelectionResult>(receiver.InstanceId, (request, _) => ValueTask.FromResult(new MedicineSelectionResult(request.Name)));
        _scope.Bind<SubmitMedicine>(endpoint.InstanceId, receiver.InstanceId);
        MviStore<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect> store = new(new(), new MedicineSearchReducer(), new MedicineSearchEffectDispatcher(endpoint));
        Search = new(store);
        _instance = new(endpoint.InstanceId, Search, [endpoint, receiver, store, Search]);
    }

    /// <summary>获取独立检索功能的视图模型。</summary>
    public CompositionDemoViewModel<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect> Search { get; }

    /// <summary>关闭独立功能。</summary>
    /// <returns>资源回收任务。</returns>
    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _instance.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
