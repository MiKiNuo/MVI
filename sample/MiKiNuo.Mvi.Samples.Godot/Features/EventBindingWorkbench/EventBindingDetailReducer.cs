using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板规约器。
/// </summary>
public sealed partial class EventBindingDetailReducer
    : MviReducerBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>处理准备动作意图。</summary>
    [MviReduce(typeof(EventBindingDetailIntent.Prepare))]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandlePrepare(
        EventBindingDetailState state,
        EventBindingDetailIntent.Prepare intent,
        IMviBusinessResult? result)
    {
        EventBindingDetailState newState = state with
        {
            PrepareCount = state.PrepareCount + 1,
            StatusText = $"准备动作：{intent.Payload.SourceName ?? "Unknown"}",
        };
        return MviReduceResult.StateAndEffect<EventBindingDetailState, EventBindingDetailEffect>(
            newState,
            new EventBindingDetailEffect.NotifyPrepare(intent.Payload.SourceName ?? "Unknown"));
    }
}
