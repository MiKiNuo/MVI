using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板变更。
/// </summary>
public abstract record EventBindingSelectionMutation : IMviMutation<EventBindingSelectionState>
{
    /// <summary>
    /// 表示设置选中患者编号的变更。
    /// </summary>
    /// <param name="Value">选中患者编号。</param>
    [MviMutation(Path = "SelectedPatientId")]
    public sealed record SetSelectedPatientId(string Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示设置选中索引的变更。
    /// </summary>
    /// <param name="Value">选中索引。</param>
    [MviMutation(Path = "SelectedIndex")]
    public sealed record SetSelectedIndex(int Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示设置事件次数的变更。
    /// </summary>
    /// <param name="Value">事件次数。</param>
    [MviMutation(Path = "EventCount")]
    public sealed record SetEventCount(int Value) : EventBindingSelectionMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingSelectionMutation;
}
