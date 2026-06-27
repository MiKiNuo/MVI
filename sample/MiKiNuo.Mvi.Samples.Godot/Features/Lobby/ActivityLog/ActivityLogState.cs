using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志状态。
/// </summary>
public sealed record ActivityLogState : IMviState
{
    /// <summary>
    /// 初始化活动日志状态。
    /// </summary>
    /// <param name="activityLog">活动日志文本。</param>
    public ActivityLogState(string activityLog)
    {
        ArgumentNullException.ThrowIfNull(activityLog);
        ActivityLog = activityLog;
    }

    /// <summary>获取活动日志文本。</summary>
    public string ActivityLog { get; init; }

    /// <summary>获取初始活动日志状态。</summary>
    public static ActivityLogState Initial { get; } = new(string.Empty);
}
