using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根变更规约器。
/// </summary>
public sealed partial class EventBindingWorkbenchMutationReducer
    : MviMutationReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchMutation, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 应用设置最后交互文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> HandleSetLastInteractionText(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchMutation.SetLastInteractionText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用累加交互次数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> HandleAddInteractionCount(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchMutation.AddInteractionCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state.Apply(mutation));
    }
}
