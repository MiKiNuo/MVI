using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板规约器。
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
            EventBindingSelectionIntent.ChangeSelection changeSelection => HandleChangeSelection(state, changeSelection),
            _ => MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(state),
        };
    }

    private static MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleChangeSelection(
        EventBindingSelectionState state,
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        string patientId = intent.Payload.SelectedValue?.ToString() ?? "-";
        int selectedIndex = intent.Payload.SelectedIndex ?? -1;
        int eventCount = state.EventCount + 1;
        string statusText = $"选择患者：{patientId}";

        EventBindingSelectionState newState = state with
        {
            SelectedPatientId = patientId,
            SelectedIndex = selectedIndex,
            EventCount = eventCount,
            StatusText = statusText,
        };
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(newState);
    }
}
