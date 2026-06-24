using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根 MVI 变更。
/// </summary>
public abstract record EventBindingWorkbenchMutation : IMviMutation<EventBindingWorkbenchState>
{
    /// <summary>
    /// 表示设置最后交互文本的变更。
    /// </summary>
    /// <param name="Value">交互文本。</param>
    [MviMutation(Path = "LastInteractionText")]
    public sealed record SetLastInteractionText(string Value) : EventBindingWorkbenchMutation;

    /// <summary>
    /// 表示累加交互次数的变更。
    /// </summary>
    /// <param name="Value">交互次数增量。</param>
    [MviMutation(Path = "InteractionCount", Op = MutationOp.Add)]
    public sealed record AddInteractionCount(int Value) : EventBindingWorkbenchMutation;
}
