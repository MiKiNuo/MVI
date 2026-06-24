using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板 MVI 变更。
/// </summary>
public abstract record EventBindingSelectionMutation : IMviMutation<EventBindingSelectionState>
{
    /// <summary>
    /// 表示设置选中任务编号的变更。
    /// </summary>
    /// <param name="Value">任务编号。</param>
    [MviMutation(Path = "SelectedMissionId")]
    public sealed record SetSelectedMissionId(string Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示设置选中索引的变更。
    /// </summary>
    /// <param name="Value">选中索引。</param>
    [MviMutation(Path = "SelectedIndex")]
    public sealed record SetSelectedIndex(int Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示累加事件次数的变更。
    /// </summary>
    /// <param name="Value">事件次数增量。</param>
    [MviMutation(Path = "EventCount", Op = MutationOp.Add)]
    public sealed record AddEventCount(int Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingSelectionMutation;
}
