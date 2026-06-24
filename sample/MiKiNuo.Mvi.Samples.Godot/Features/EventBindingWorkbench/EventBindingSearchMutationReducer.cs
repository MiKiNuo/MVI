using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板变更规约器。
/// </summary>
public sealed partial class EventBindingSearchMutationReducer
    : MviMutationReducerBase<EventBindingSearchState, EventBindingSearchMutation, EventBindingSearchEffect>
{
    /// <summary>
    /// 应用设置查询文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleSetQueryText(
        EventBindingSearchState state,
        EventBindingSearchMutation.SetQueryText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用累加事件次数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleAddEventCount(
        EventBindingSearchState state,
        EventBindingSearchMutation.AddEventCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置状态文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleSetStatusText(
        EventBindingSearchState state,
        EventBindingSearchMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(state.Apply(mutation));
    }
}
