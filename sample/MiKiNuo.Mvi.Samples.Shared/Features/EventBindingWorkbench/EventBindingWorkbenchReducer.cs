using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根规约器。
/// </summary>
public sealed class EventBindingWorkbenchReducer
    : MviReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> Reduce(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            EventBindingWorkbenchIntent.RecordInteraction recordInteraction => HandleRecordInteraction(state, recordInteraction),
            _ => MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state),
        };
    }

    private static MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> HandleRecordInteraction(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent.RecordInteraction intent)
    {
        string interactionText = $"{intent.SourceComponent}/{intent.ActionKey}: {intent.ContextText}";
        EventBindingWorkbenchState newState = state with
        {
            LastInteractionText = interactionText,
            InteractionCount = state.InteractionCount + 1,
        };
        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(newState);
    }
}
