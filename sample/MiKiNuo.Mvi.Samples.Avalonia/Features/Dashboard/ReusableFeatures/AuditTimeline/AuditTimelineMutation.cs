using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示审计时间线 MVI 变更。
/// </summary>
public abstract record AuditTimelineMutation : IMviMutation<AuditTimelineState>
{
    /// <summary>
    /// 表示设置最新事件的变更。
    /// </summary>
    /// <param name="Value">最新事件文本。</param>
    [MviMutation(Path = "LatestEvent")]
    public sealed record SetLatestEvent(string Value) : AuditTimelineMutation;

    /// <summary>
    /// 表示设置条目数的变更。
    /// </summary>
    /// <param name="Value">条目数。</param>
    [MviMutation(Path = "EntryCount")]
    public sealed record SetEntryCount(int Value) : AuditTimelineMutation;

    /// <summary>
    /// 表示设置条目文本的变更。
    /// </summary>
    /// <param name="Value">条目文本。</param>
    [MviMutation(Path = "EntriesText")]
    public sealed record SetEntriesText(string Value) : AuditTimelineMutation;

    /// <summary>
    /// 表示设置可清空状态的变更。
    /// </summary>
    /// <param name="Value">是否可清空。</param>
    [MviMutation(Path = "CanClear")]
    public sealed record SetCanClear(bool Value) : AuditTimelineMutation;
}
