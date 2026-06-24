using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板变更规约器。
/// </summary>
public sealed partial class EventBindingSelectionMutationReducer
    : MviMutationReducerBase<EventBindingSelectionState, EventBindingSelectionMutation, EventBindingSelectionEffect>
{
    /// <summary>
    /// 应用设置选中患者编号变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleSetSelectedPatientId(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.SetSelectedPatientId mutation)
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
    /// 应用设置事件次数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleSetEventCount(
        EventBindingSelectionState state,
        EventBindingSelectionMutation.SetEventCount mutation)
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
