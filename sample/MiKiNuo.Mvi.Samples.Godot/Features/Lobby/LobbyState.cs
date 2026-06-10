using System;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 MVI 状态。
/// <para>
/// 不再持有任何 <c>*ViewModel</c> 引用：
/// </para>
/// <list type="bullet">
/// <item>3 个常驻子 VM（玩家头部 / 大厅菜单 / 活动日志）：由 <see cref="LobbyViewModel"/> 构造函数注入并暴露为只读属性。</item>
/// <item>5 个互斥面板 VM（任务大厅 / 英雄队伍 / 背包仓库 / 锻造工坊 / 战斗准备）：通过 <see cref="ILobbyPanelFactory"/> 按 <see cref="CurrentPanel"/> 解析。</item>
/// </list>
/// </summary>
public sealed record LobbyState : IMviState
{
    /// <summary>
    /// 初始化游戏大厅 MVI 状态。
    /// </summary>
    public LobbyState(
        string playerName,
        int playerLevel,
        int gold,
        int stamina,
        string currentPanel,
        string currentPanelTitle,
        string selectedMission,
        string missionProgress,
        int heroTeamPower,
        int warriorLevel,
        int mageLevel,
        int archerLevel,
        int potionCount,
        int oreCount,
        int crystalCount,
        int forgeScore,
        string battleReadyText,
        string activityLog,
        bool canExecuteCommands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPanel);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPanelTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedMission);
        ArgumentException.ThrowIfNullOrWhiteSpace(missionProgress);
        ArgumentException.ThrowIfNullOrWhiteSpace(battleReadyText);
        ArgumentNullException.ThrowIfNull(activityLog);
        PlayerName = playerName;
        PlayerLevel = playerLevel;
        Gold = gold;
        Stamina = stamina;
        CurrentPanel = currentPanel;
        CurrentPanelTitle = currentPanelTitle;
        SelectedMission = selectedMission;
        MissionProgress = missionProgress;
        HeroTeamPower = heroTeamPower;
        WarriorLevel = warriorLevel;
        MageLevel = mageLevel;
        ArcherLevel = archerLevel;
        PotionCount = potionCount;
        OreCount = oreCount;
        CrystalCount = crystalCount;
        ForgeScore = forgeScore;
        BattleReadyText = battleReadyText;
        ActivityLog = activityLog;
        CanExecuteCommands = canExecuteCommands;
    }

    /// <summary>获取玩家名称。</summary>
    public string PlayerName { get; init; }

    /// <summary>获取玩家等级。</summary>
    public int PlayerLevel { get; init; }

    /// <summary>获取金币数量。</summary>
    public int Gold { get; init; }

    /// <summary>获取体力值。</summary>
    public int Stamina { get; init; }

    /// <summary>获取当前大厅面板键。</summary>
    public string CurrentPanel { get; init; }

    /// <summary>获取当前大厅面板标题。</summary>
    public string CurrentPanelTitle { get; init; }

    /// <summary>获取当前选中任务。</summary>
    public string SelectedMission { get; init; }

    /// <summary>获取任务进度说明。</summary>
    public string MissionProgress { get; init; }

    /// <summary>获取英雄队伍战力。</summary>
    public int HeroTeamPower { get; init; }

    /// <summary>获取战士等级。</summary>
    public int WarriorLevel { get; init; }

    /// <summary>获取法师等级。</summary>
    public int MageLevel { get; init; }

    /// <summary>获取弓手等级。</summary>
    public int ArcherLevel { get; init; }

    /// <summary>获取药水数量。</summary>
    public int PotionCount { get; init; }

    /// <summary>获取矿石数量。</summary>
    public int OreCount { get; init; }

    /// <summary>获取水晶数量。</summary>
    public int CrystalCount { get; init; }

    /// <summary>获取锻造评分。</summary>
    public int ForgeScore { get; init; }

    /// <summary>获取战斗准备摘要。</summary>
    public string BattleReadyText { get; init; }

    /// <summary>获取活动日志。</summary>
    public string ActivityLog { get; init; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    public bool CanExecuteCommands { get; init; }

    /// <summary>获取初始状态。</summary>
    public static LobbyState Initial { get; } = new(
        playerName: "未登录指挥官",
        playerLevel: 1,
        gold: 0,
        stamina: 0,
        currentPanel: LobbyPanelKeys.MissionBoard,
        currentPanelTitle: "任务大厅",
        selectedMission: "森林巡逻",
        missionProgress: "等待登录后选择任务。",
        heroTeamPower: 36,
        warriorLevel: 3,
        mageLevel: 2,
        archerLevel: 2,
        potionCount: 2,
        oreCount: 4,
        crystalCount: 1,
        forgeScore: 0,
        battleReadyText: "等待大厅初始化。",
        activityLog: string.Empty,
        canExecuteCommands: true);
}
