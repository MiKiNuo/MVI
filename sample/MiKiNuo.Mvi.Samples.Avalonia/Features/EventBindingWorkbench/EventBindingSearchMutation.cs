using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板变更。
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
    /// 表示设置上次查询文本的变更。
    /// </summary>
    /// <param name="Value">上次查询文本。</param>
    [MviMutation(Path = "PreviousQueryText")]
    public sealed record SetPreviousQueryText(string Value) : EventBindingSearchMutation;

    /// <summary>
    /// 表示设置事件次数的变更。
    /// </summary>
    /// <param name="Value">事件次数。</param>
    [MviMutation(Path = "EventCount")]
    public sealed record SetEventCount(int Value) : EventBindingSearchMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingSearchMutation;
}
