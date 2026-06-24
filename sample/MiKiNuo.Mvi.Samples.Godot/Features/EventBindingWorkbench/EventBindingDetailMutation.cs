using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板 MVI 变更。
/// </summary>
public abstract record EventBindingDetailMutation : IMviMutation<EventBindingDetailState>
{
    /// <summary>
    /// 表示累加准备次数的变更。
    /// </summary>
    /// <param name="Value">准备次数增量。</param>
    [MviMutation(Path = "PrepareCount", Op = MutationOp.Add)]
    public sealed record AddPrepareCount(int Value) : EventBindingDetailMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingDetailMutation;
}
