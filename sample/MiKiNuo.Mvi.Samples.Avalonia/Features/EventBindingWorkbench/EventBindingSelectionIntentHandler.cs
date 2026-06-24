using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板意图处理器。
/// </summary>
public sealed class EventBindingSelectionIntentHandler
    : IMviIntentHandler<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionMutation, EventBindingSelectionEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<EventBindingSelectionMutation, EventBindingSelectionEffect>> HandleAsync(
        EventBindingSelectionState state,
        EventBindingSelectionIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<EventBindingSelectionMutation, EventBindingSelectionEffect> result = intent switch
        {
            EventBindingSelectionIntent.ChangeSelection changeSelection => HandleChangeSelection(state, changeSelection),
            _ => MviHandleResult.Empty<EventBindingSelectionMutation, EventBindingSelectionEffect>(),
        };
        return new ValueTask<MviHandleResult<EventBindingSelectionMutation, EventBindingSelectionEffect>>(result);
    }

    private static MviHandleResult<EventBindingSelectionMutation, EventBindingSelectionEffect> HandleChangeSelection(
        EventBindingSelectionState state,
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        string patientId = intent.Payload.SelectedValue?.ToString() ?? "-";
        int selectedIndex = intent.Payload.SelectedIndex ?? -1;
        int eventCount = state.EventCount + 1;
        string statusText = $"选择患者：{patientId}";

        EventBindingSelectionMutation[] mutations =
        {
            new EventBindingSelectionMutation.SetSelectedPatientId(patientId),
            new EventBindingSelectionMutation.SetSelectedIndex(selectedIndex),
            new EventBindingSelectionMutation.SetEventCount(eventCount),
            new EventBindingSelectionMutation.SetStatusText(statusText),
        };
        EventBindingSelectionEffect[] effects = { new EventBindingSelectionEffect.NotifySelectionChanged(patientId) };
        return MviHandleResult.MutationsAndEffects<EventBindingSelectionMutation, EventBindingSelectionEffect>(mutations, effects);
    }
}
