using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板规约器。
/// </summary>
public sealed partial class EventBindingSelectionReducer
    : MviReducerBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 处理选择变更意图。
    /// </summary>
    [MviReduce(typeof(EventBindingSelectionIntent.ChangeSelection))]
    private MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleChangeSelection(
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
