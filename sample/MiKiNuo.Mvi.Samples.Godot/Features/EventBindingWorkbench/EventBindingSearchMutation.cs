using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板 MVI 变更。
/// </summary>
public abstract record EventBindingSearchMutation : IMviMutation<EventBindingSearchState>
{
    /// <summary>
    /// 表示设置查询文本的变更。
    /// </summary>
    /// <param name="Value">查询文本。</param>
    [MviMutation(Path = "QueryText")]
    public sealed record SetQueryText(string Value) : EventBindingSearchMutation;

    /// <summary>
    /// 表示累加事件次数的变更。
    /// </summary>
    /// <param name="Value">事件次数增量。</param>
    [MviMutation(Path = "EventCount", Op = MutationOp.Add)]
    public sealed record AddEventCount(int Value) : EventBindingSearchMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingSearchMutation;
}
