using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板变更规约器。
/// </summary>
public sealed partial class EventBindingDetailMutationReducer
    : MviMutationReducerBase<EventBindingDetailState, EventBindingDetailMutation, EventBindingDetailEffect>
{
    /// <summary>
    /// 应用累加准备次数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandleAddPrepareCount(
        EventBindingDetailState state,
        EventBindingDetailMutation.AddPrepareCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置状态文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandleSetStatusText(
        EventBindingDetailState state,
        EventBindingDetailMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(state.Apply(mutation));
    }
}
