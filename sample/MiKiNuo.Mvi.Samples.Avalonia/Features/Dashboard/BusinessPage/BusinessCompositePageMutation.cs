using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面 MVI 变更。
/// </summary>
public abstract record BusinessCompositePageMutation : IMviMutation<BusinessCompositePageState>
{
    /// <summary>
    /// 表示设置当前业务上下文的变更。
    /// </summary>
    /// <param name="Value">业务上下文。</param>
    [MviMutation(Path = "ActiveContext")]
    public sealed record SetActiveContext(string Value) : BusinessCompositePageMutation;

    /// <summary>
    /// 表示设置当前流程状态的变更。
    /// </summary>
    /// <param name="Value">流程状态。</param>
    [MviMutation(Path = "FlowStatus")]
    public sealed record SetFlowStatus(string Value) : BusinessCompositePageMutation;

    /// <summary>
    /// 表示设置交互日志的变更。
    /// </summary>
    /// <param name="Value">交互日志。</param>
    [MviMutation(Path = "InteractionLog")]
    public sealed record SetInteractionLog(string Value) : BusinessCompositePageMutation;
}
