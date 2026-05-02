using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 意图。
/// </summary>
public abstract partial record AuditTimelineIntent : IMviIntent
{
    /// <summary>
    /// 表示追加审计记录意图。
    /// </summary>
    /// <param name="Message">审计消息。</param>
    public sealed partial record AppendEntry(string Message) : AuditTimelineIntent;

    /// <summary>
    /// 表示清空审计记录意图。
    /// </summary>
    public sealed partial record ClearEntries : AuditTimelineIntent;
}
