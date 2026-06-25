using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根规约器。
/// </summary>
public sealed partial class EventBindingWorkbenchReducer
    : MviReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>处理记录交互意图。</summary>
    [MviReduce(typeof(EventBindingWorkbenchIntent.RecordInteraction))]
    private MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> HandleRecordInteraction(
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
