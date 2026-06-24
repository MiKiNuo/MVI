using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板变更。
/// </summary>
public abstract record EventBindingDetailMutation : IMviMutation<EventBindingDetailState>
{
    /// <summary>
    /// 表示设置最后指针文本的变更。
    /// </summary>
    /// <param name="Value">最后指针文本。</param>
    [MviMutation(Path = "LastPointerText")]
    public sealed record SetLastPointerText(string Value) : EventBindingDetailMutation;

    /// <summary>
    /// 表示设置刷新次数的变更。
    /// </summary>
    /// <param name="Value">刷新次数。</param>
    [MviMutation(Path = "RefreshCount")]
    public sealed record SetRefreshCount(int Value) : EventBindingDetailMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : EventBindingDetailMutation;
}
