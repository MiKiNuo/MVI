using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板规约器。
/// </summary>
public sealed class EventBindingSelectionReducer
    : MviReducerBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> Reduce(
        EventBindingSelectionState state,
        EventBindingSelectionIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            EventBindingSelectionIntent.ChangeSelection changeSelection => MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(
                state with
                {
                    SelectedMissionId = changeSelection.Payload.SelectedValue?.ToString() ?? "-",
                    SelectedIndex = changeSelection.Payload.SelectedIndex ?? -1,
                    EventCount = state.EventCount + 1,
                    StatusText = $"选择任务：{changeSelection.Payload.SelectedValue?.ToString() ?? "-"}",
                }),
            _ => MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state),
        };
    }
}
