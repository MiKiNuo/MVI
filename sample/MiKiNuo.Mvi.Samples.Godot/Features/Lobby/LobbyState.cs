using System;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料子状态（玩家头部 / 任务大厅 / 英雄队伍 / 背包共享）。
/// </summary>
public sealed record LobbyPlayer : IMviState
{
    /// <summary>初始化玩家资料子状态。</summary>
    public LobbyPlayer(string playerName, int playerLevel, int gold, int stamina)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        PlayerName = playerName;
        PlayerLevel = playerLevel;
        Gold = gold;
        Stamina = stamina;
    }

    /// <summary>获取玩家名称。</summary>
    public string PlayerName { get; init; }

    /// <summary>获取玩家等级。</summary>
    public int PlayerLevel { get; init; }

    /// <summary>获取金币数量。</summary>
    public int Gold { get; init; }

    /// <summary>获取体力值。</summary>
    public int Stamina { get; init; }

    /// <summary>获取初始玩家资料。</summary>
    public static LobbyPlayer Initial { get; } = new("未登录指挥官", 1, 0, 0);
}

/// <summary>
/// 表示大厅导航子状态（当前面板键 + 标题）。
/// </summary>
public sealed record LobbyNavigation : IMviState
{
    /// <summary>初始化大厅导航子状态。</summary>
    public LobbyNavigation(string currentPanel, string currentPanelTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPanel);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPanelTitle);
        CurrentPanel = currentPanel;
        CurrentPanelTitle = currentPanelTitle;
    }

    /// <summary>获取当前大厅面板键。</summary>
    public string CurrentPanel { get; init; }

    /// <summary>获取当前大厅面板标题。</summary>
    public string CurrentPanelTitle { get; init; }

    /// <summary>获取初始导航状态。</summary>
    public static LobbyNavigation Initial { get; } = new(LobbyPanelKeys.MissionBoard, "任务大厅");
}

/// <summary>
/// 表示任务大厅子状态（选中任务 + 进度说明）。
/// </summary>
public sealed record LobbyMission : IMviState
{
    /// <summary>初始化任务大厅子状态。</summary>
    public LobbyMission(string selectedMission, string missionProgress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedMission);
        ArgumentException.ThrowIfNullOrWhiteSpace(missionProgress);
        SelectedMission = selectedMission;
        MissionProgress = missionProgress;
    }

    /// <summary>获取当前选中任务。</summary>
    public string SelectedMission { get; init; }

    /// <summary>获取任务进度说明。</summary>
    public string MissionProgress { get; init; }

    /// <summary>获取初始任务状态。</summary>
    public static LobbyMission Initial { get; } = new("森林巡逻", "等待登录后选择任务。");
}

/// <summary>
/// 表示英雄队伍子状态（战力 + 三英雄等级）。
/// </summary>
public sealed record LobbyHeroRoster : IMviState
{
    /// <summary>初始化英雄队伍子状态。</summary>
    public LobbyHeroRoster(int heroTeamPower, int warriorLevel, int mageLevel, int archerLevel)
    {
        HeroTeamPower = heroTeamPower;
        WarriorLevel = warriorLevel;
        MageLevel = mageLevel;
        ArcherLevel = archerLevel;
    }

    /// <summary>获取英雄队伍战力。</summary>
    public int HeroTeamPower { get; init; }

    /// <summary>获取战士等级。</summary>
    public int WarriorLevel { get; init; }

    /// <summary>获取法师等级。</summary>
    public int MageLevel { get; init; }

    /// <summary>获取弓手等级。</summary>
    public int ArcherLevel { get; init; }

    /// <summary>获取初始英雄队伍状态。</summary>
    public static LobbyHeroRoster Initial { get; } = new(36, 3, 2, 2);
}

/// <summary>
/// 表示背包仓库子状态（药水 + 矿石 + 水晶 + 锻造评分）。
/// </summary>
public sealed record LobbyInventory : IMviState
{
    /// <summary>初始化背包仓库子状态。</summary>
    public LobbyInventory(int potionCount, int oreCount, int crystalCount, int forgeScore)
    {
        PotionCount = potionCount;
        OreCount = oreCount;
        CrystalCount = crystalCount;
        ForgeScore = forgeScore;
    }

    /// <summary>获取药水数量。</summary>
    public int PotionCount { get; init; }

    /// <summary>获取矿石数量。</summary>
    public int OreCount { get; init; }

    /// <summary>获取水晶数量。</summary>
    public int CrystalCount { get; init; }

    /// <summary>获取锻造评分。</summary>
    public int ForgeScore { get; init; }

    /// <summary>获取初始背包状态。</summary>
    public static LobbyInventory Initial { get; } = new(2, 4, 1, 0);
}

/// <summary>
/// 表示游戏大厅 MVI 状态。
/// <para>
/// 按面板关注点拆分为 5 个子状态记录，避免 God Object 反模式：
/// </para>
/// <list type="bullet">
/// <item><see cref="Player"/>：玩家资料（名称 / 等级 / 金币 / 体力）。</item>
/// <item><see cref="Navigation"/>：大厅导航（当前面板键 + 标题）。</item>
/// <item><see cref="Mission"/>：任务大厅（选中任务 + 进度）。</item>
/// <item><see cref="HeroRoster"/>：英雄队伍（战力 + 三英雄等级）。</item>
/// <item><see cref="Inventory"/>：背包仓库（药水 / 矿石 / 水晶 / 锻造评分）。</item>
/// </list>
/// <para>
/// 顶层保留 3 个跨面板字段：<see cref="BattleReadyText"/>（由子状态派生）、
/// <see cref="ActivityLog"/>（全局日志）、<see cref="CanExecuteCommands"/>（全局开关）。
/// </para>
/// <para>
/// 不再持有任何 <c>*ViewModel</c> 引用：
/// </para>
/// <list type="bullet">
/// <item>3 个常驻子 VM（玩家头部 / 大厅菜单 / 活动日志）：由 <see cref="LobbyViewModel"/> 构造函数注入并暴露为只读属性。</item>
/// <item>5 个互斥面板 VM（任务大厅 / 英雄队伍 / 背包仓库 / 锻造工坊 / 战斗准备）：通过 <see cref="ILobbyPanelFactory"/> 按 <c>Navigation.CurrentPanel</c> 解析。</item>
/// </list>
/// </summary>
public sealed record LobbyState : IMviState
{
    /// <summary>
    /// 初始化游戏大厅 MVI 状态。
    /// </summary>
    public LobbyState(
        LobbyPlayer player,
        LobbyNavigation navigation,
        LobbyMission mission,
        LobbyHeroRoster heroRoster,
        LobbyInventory inventory,
        string battleReadyText,
        string activityLog,
        bool canExecuteCommands)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(mission);
        ArgumentNullException.ThrowIfNull(heroRoster);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentException.ThrowIfNullOrWhiteSpace(battleReadyText);
        ArgumentNullException.ThrowIfNull(activityLog);
        Player = player;
        Navigation = navigation;
        Mission = mission;
        HeroRoster = heroRoster;
        Inventory = inventory;
        BattleReadyText = battleReadyText;
        ActivityLog = activityLog;
        CanExecuteCommands = canExecuteCommands;
    }

    /// <summary>获取玩家资料子状态。</summary>
    public LobbyPlayer Player { get; init; }

    /// <summary>获取大厅导航子状态。</summary>
    public LobbyNavigation Navigation { get; init; }

    /// <summary>获取任务大厅子状态。</summary>
    public LobbyMission Mission { get; init; }

    /// <summary>获取英雄队伍子状态。</summary>
    public LobbyHeroRoster HeroRoster { get; init; }

    /// <summary>获取背包仓库子状态。</summary>
    public LobbyInventory Inventory { get; init; }

    /// <summary>获取战斗准备摘要。</summary>
    public string BattleReadyText { get; init; }

    /// <summary>获取活动日志。</summary>
    public string ActivityLog { get; init; }

    /// <summary>获取大厅命令是否允许执行。</summary>
    public bool CanExecuteCommands { get; init; }

    /// <summary>获取初始状态。</summary>
    public static LobbyState Initial { get; } = new(
        player: LobbyPlayer.Initial,
        navigation: LobbyNavigation.Initial,
        mission: LobbyMission.Initial,
        heroRoster: LobbyHeroRoster.Initial,
        inventory: LobbyInventory.Initial,
        battleReadyText: "等待大厅初始化。",
        activityLog: string.Empty,
        canExecuteCommands: true);
}
