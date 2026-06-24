using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板变更规约器。
/// </summary>
public sealed partial class EventBindingSelectionMutationReducer
    : MviMutationReducerBase<EventBindingSelectionState, EventBindingSelectionMutation, EventBindingSelectionEffect>
{
    /// <summary>
    /// 应用设置选中任务编号变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleSetSelectedMissionId(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.SetSelectedMissionId mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置选中索引变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleSetSelectedIndex(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.SetSelectedIndex mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用累加事件次数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleAddEventCount(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.AddEventCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置状态文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleSetStatusText(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state.Apply(mutation));
    }
}
