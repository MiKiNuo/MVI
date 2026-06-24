using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板意图处理器。
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
            EventBindingSelectionIntent.ChangeSelection changeSelection => HandleChangeSelection(changeSelection),
            _ => MviHandleResult.Empty<EventBindingSelectionMutation, EventBindingSelectionEffect>(),
        };
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 处理选择变化意图。
    /// </summary>
    /// <param name="intent">选择变化意图。</param>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<EventBindingSelectionMutation, EventBindingSelectionEffect> HandleChangeSelection(
        EventBindingSelectionIntent.ChangeSelection intent)
    {
        string missionId = intent.Payload.SelectedValue?.ToString() ?? "-";
        int selectedIndex = intent.Payload.SelectedIndex ?? -1;
        EventBindingSelectionMutation[] mutations = new EventBindingSelectionMutation[]
        {
            new EventBindingSelectionMutation.SetSelectedMissionId(missionId),
            new EventBindingSelectionMutation.SetSelectedIndex(selectedIndex),
            new EventBindingSelectionMutation.AddEventCount(1),
            new EventBindingSelectionMutation.SetStatusText($"选择任务：{missionId}"),
        };
        EventBindingSelectionEffect[] effects = new EventBindingSelectionEffect[]
        {
            new EventBindingSelectionEffect.NotifySelectionChanged(missionId),
        };
        return MviHandleResult.MutationsAndEffects<EventBindingSelectionMutation, EventBindingSelectionEffect>(mutations, effects);
    }
}
