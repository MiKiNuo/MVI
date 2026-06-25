using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板规约器。
/// </summary>
public sealed partial class EventBindingSelectionReducer
    : MviReducerBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>处理选择变化意图。</summary>
    [MviReduce(typeof(EventBindingSelectionIntent.ChangeSelection))]
    private MviReduceResult<EventBindingSelectionState, EventBindingSelectionEffect> HandleChangeSelection(
        EventBindingSelectionState state,
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        return MviReduceResult.State<EventBindingSelectionState, EventBindingSelectionEffect>(
            state with
            {
                SelectedMissionId = intent.Payload.SelectedValue?.ToString() ?? "-",
                SelectedIndex = intent.Payload.SelectedIndex ?? -1,
                EventCount = state.EventCount + 1,
                StatusText = $"选择任务：{intent.Payload.SelectedValue?.ToString() ?? "-"}",
            });
    }
}
