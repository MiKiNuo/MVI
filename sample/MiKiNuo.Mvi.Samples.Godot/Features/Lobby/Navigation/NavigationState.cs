using System;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅导航 MVI 状态。
/// </summary>
public sealed record NavigationState : IMviState
{
    /// <summary>初始化大厅导航状态。</summary>
    /// <param name="currentPanel">当前面板。</param>
    /// <param name="currentPanelTitle">当前面板标题。</param>
    public NavigationState(LobbyPanel currentPanel, string currentPanelTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPanelTitle);
        CurrentPanel = currentPanel;
        CurrentPanelTitle = currentPanelTitle;
    }

    /// <summary>获取当前面板。</summary>
    public LobbyPanel CurrentPanel { get; init; }

    /// <summary>获取当前面板标题。</summary>
    public string CurrentPanelTitle { get; init; }

    /// <summary>获取初始导航状态。</summary>
    public static NavigationState Initial { get; } = new(LobbyPanel.MissionBoard, "任务大厅");
}
