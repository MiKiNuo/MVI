using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="PageKey">所属页面键。</param>
/// <param name="LatestEvent">最新审计事件。</param>
/// <param name="EntryCount">审计条目数量。</param>
/// <param name="EntriesText">审计条目文本。</param>
/// <param name="CanClear">是否允许清空审计条目。</param>
public sealed record AuditTimelineState(
    string Title,
    string PageKey,
    string LatestEvent,
    int EntryCount,
    string EntriesText,
    bool CanClear) : IMviState
{
    /// <summary>
    /// 创建指定页面的初始审计时间线状态。
    /// </summary>
    /// <param name="pageKey">所属页面键。</param>
    /// <returns>初始状态。</returns>
    public static AuditTimelineState CreateInitial(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        return new AuditTimelineState(
            "复用审计时间线 MVI",
            pageKey,
            "等待业务流转或中间件诊断。",
            0,
            "暂无审计记录。",
            false);
    }
}
