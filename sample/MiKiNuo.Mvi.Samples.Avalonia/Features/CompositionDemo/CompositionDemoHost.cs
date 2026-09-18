using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.CompositionDemo;

/// <summary>三个演示功能共用的纯视图适配，只访问自己的存储。</summary>
/// <typeparam name="TState">本实例状态。</typeparam>
/// <typeparam name="TIntent">本实例意图。</typeparam>
/// <typeparam name="TEffect">本实例副作用。</typeparam>
public class CompositionDemoViewModel<TState, TIntent, TEffect>(IMviStore<TState, TIntent, TEffect> store)
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

/// <summary>药品检索功能的视图模型。</summary>
/// <param name="store">检索状态存储。</param>
public sealed class MedicineSearchViewModel(IMviStore<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect> store)
    : CompositionDemoViewModel<MedicineSearchState, MedicineSearchIntent, MedicineSearchEffect>(store);

/// <summary>药品明细功能的视图模型。</summary>
/// <param name="store">明细状态存储。</param>
public sealed class MedicationDetailsViewModel(IMviStore<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect> store)
    : CompositionDemoViewModel<MedicationDetailsState, MedicationDetailsIntent, MedicationDetailsEffect>(store);

/// <summary>处方工作区功能的视图模型。</summary>
/// <param name="store">工作区状态存储。</param>
public sealed class PrescriptionWorkspaceViewModel(IMviStore<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect> store)
    : CompositionDemoViewModel<PrescriptionWorkspaceState, PrescriptionWorkspaceIntent, UnitEffect>(store);

/// <summary>独立检索组合声明：检索与明细的最小接线。</summary>
[MviComposition(typeof(MedicationDetailsReducer), typeof(MedicineSearchReducer))]
public sealed partial class MedicineSearchComposition
{
}

/// <summary>处方组合声明：工作区订阅明细事实，检索经路由提交到明细。</summary>
[MviComposition(typeof(PrescriptionWorkspaceReducer), typeof(MedicationDetailsReducer), typeof(MedicineSearchReducer))]
public sealed partial class PrescriptionComposition
{
}
