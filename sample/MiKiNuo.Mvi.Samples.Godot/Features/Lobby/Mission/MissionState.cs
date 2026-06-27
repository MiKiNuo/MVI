using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务状态。
/// </summary>
public sealed record MissionState : IMviState
{
    /// <summary>
    /// 初始化任务状态。
    /// </summary>
    /// <param name="selectedMission">已选任务名称。</param>
    /// <param name="missionProgress">任务进度文本。</param>
    public MissionState(string selectedMission, string missionProgress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedMission);
        ArgumentException.ThrowIfNullOrWhiteSpace(missionProgress);
        SelectedMission = selectedMission;
        MissionProgress = missionProgress;
    }

    /// <summary>获取已选任务名称。</summary>
    public string SelectedMission { get; init; }

    /// <summary>获取任务进度文本。</summary>
    public string MissionProgress { get; init; }

    /// <summary>获取初始任务状态。</summary>
    public static MissionState Initial { get; } = new("森林巡逻", "等待登录后选择任务。");
}
