using System;
using System.Globalization;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅规约器。
/// </summary>
public sealed class LobbyReducer
    : MviReducerBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<LobbyState, LobbyEffect> Reduce(
        LobbyState state,
        LobbyIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            LobbyIntent.SetPlayer setPlayer => HandleSetPlayer(state, setPlayer.Profile),
            LobbyIntent.SelectMissionBoard => SelectPanel(state, LobbyPanelKeys.MissionBoard, "任务大厅", "切换到任务大厅。不同子 MVI 会收到父状态变化。 "),
            LobbyIntent.SelectHeroRoster => SelectPanel(state, LobbyPanelKeys.HeroRoster, "英雄队伍", "切换到英雄队伍，训练会影响战斗准备页。 "),
            LobbyIntent.SelectInventory => SelectPanel(state, LobbyPanelKeys.Inventory, "背包仓库", "切换到背包仓库，药水会影响战斗准备页。 "),
            LobbyIntent.SelectForgeLab => SelectPanel(state, LobbyPanelKeys.ForgeLab, "锻造工坊", "切换到锻造工坊，用同一个 GameLogicService 验证逻辑复用。 "),
            LobbyIntent.SelectBattlePrep => SelectPanel(state, LobbyPanelKeys.BattlePrep, "战斗准备", "切换到战斗准备，汇总任务、英雄、背包等子 MVI 数据。 "),
            LobbyIntent.PlayerSet playerSet => MviReduceResult.State<LobbyState, LobbyEffect>(
                state with { BattleReadyText = playerSet.BattleReadyText }),
            LobbyIntent.MissionAccepted accepted => HandleMissionAccepted(state, accepted),
            LobbyIntent.MissionAcceptFailed failed => MviReduceResult.State<LobbyState, LobbyEffect>(
                AppendActivityLog(state, failed.ErrorMessage ?? "接受任务失败。")),
            LobbyIntent.MissionCompleted completed => HandleMissionCompleted(state, completed),
            LobbyIntent.HeroTrained trained => HandleHeroTrained(state, trained),
            LobbyIntent.HeroTrainFailed failed => MviReduceResult.State<LobbyState, LobbyEffect>(
                AppendActivityLog(state, failed.ErrorMessage ?? "训练失败。")),
            LobbyIntent.PotionUsed used => HandlePotionUsed(state, used),
            LobbyIntent.PotionUseFailed failed => MviReduceResult.State<LobbyState, LobbyEffect>(
                AppendActivityLog(state, failed.ErrorMessage ?? "使用药水失败。")),
            LobbyIntent.GoldBoxOpened opened => HandleGoldBoxOpened(state, opened),
            LobbyIntent.Forged forged => HandleForged(state, forged),
            LobbyIntent.ForgeFailed failed => MviReduceResult.State<LobbyState, LobbyEffect>(
                AppendActivityLog(state, failed.ErrorMessage ?? "锻造失败。")),
            LobbyIntent.BattlePrepared prepared => MviReduceResult.State<LobbyState, LobbyEffect>(
                state with
                {
                    BattleReadyText = prepared.BattleReadyText,
                    ActivityLog = AppendLogEntry(state.ActivityLog, "战斗准备子 MVI 汇总任务、英雄、背包数据。"),
                }),
            LobbyIntent.Logout => MviReduceResult.State<LobbyState, LobbyEffect>(
                AppendActivityLog(state, "大厅请求退出到登录页。")),
            _ => MviReduceResult.State<LobbyState, LobbyEffect>(state),
        };
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleSetPlayer(LobbyState state, PlayerProfile profile)
    {
        LobbyPlayer newPlayer = new(profile.PlayerName, profile.Level, profile.Gold, profile.Stamina);
        LobbyNavigation newNavigation = new(LobbyPanelKeys.MissionBoard, "任务大厅");
        LobbyMission newMission = state.Mission with
        {
            MissionProgress = "登录成功，任务大厅等待指挥官选择任务。",
        };
        LobbyState nextState = state with
        {
            Player = newPlayer,
            Navigation = newNavigation,
            Mission = newMission,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"Login MVI 传入玩家资料：{profile.PlayerName}。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> SelectPanel(
        LobbyState state,
        string panel,
        string title,
        string log)
    {
        LobbyNavigation newNavigation = new(panel, title);
        LobbyState nextState = state with
        {
            Navigation = newNavigation,
            ActivityLog = AppendLogEntry(state.ActivityLog, log),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleMissionAccepted(
        LobbyState state,
        LobbyIntent.MissionAccepted accepted)
    {
        LobbyMission newMission = new(
            accepted.MissionName,
            $"已接受 {accepted.MissionName}，消耗体力 {accepted.StaminaCost}，预计奖励 {accepted.Reward}。");
        LobbyState nextState = state with
        {
            Player = state.Player with { Stamina = accepted.NewStamina },
            Mission = newMission,
            BattleReadyText = accepted.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"任务子 MVI 接受 {accepted.MissionName}，父 Lobby 状态同步体力和任务。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleMissionCompleted(
        LobbyState state,
        LobbyIntent.MissionCompleted completed)
    {
        LobbyMission newMission = state.Mission with
        {
            MissionProgress = $"{state.Mission.SelectedMission} 已完成，获得金币 {completed.Reward}。奖励由 GameLogicService 计算。",
        };
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold + completed.Reward },
            Mission = newMission,
            BattleReadyText = completed.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"任务完成：{state.Mission.SelectedMission}，奖励 {completed.Reward}。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleHeroTrained(
        LobbyState state,
        LobbyIntent.HeroTrained trained)
    {
        LobbyHeroRoster leveledRoster = ApplyHeroLevel(state.HeroRoster, trained.Kind, trained.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        LobbyHeroRoster newRoster = leveledRoster with { HeroTeamPower = nextPower };
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold - trained.Cost },
            HeroRoster = newRoster,
            BattleReadyText = trained.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"英雄子 MVI 训练{trained.HeroName}，消耗 {trained.Cost} 金币，战力变为 {nextPower}。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandlePotionUsed(
        LobbyState state,
        LobbyIntent.PotionUsed used)
    {
        LobbyInventory newInventory = state.Inventory with { PotionCount = used.NewPotionCount };
        LobbyState nextState = state with
        {
            Inventory = newInventory,
            Player = state.Player with { Stamina = used.NewStamina },
            BattleReadyText = used.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, "背包子 MVI 使用药水，体力恢复 20。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleGoldBoxOpened(
        LobbyState state,
        LobbyIntent.GoldBoxOpened opened)
    {
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold + opened.Gold },
            ActivityLog = AppendLogEntry(state.ActivityLog, $"背包子 MVI 打开金币箱，金币增加 {opened.Gold}。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static MviReduceResult<LobbyState, LobbyEffect> HandleForged(
        LobbyState state,
        LobbyIntent.Forged forged)
    {
        LobbyInventory newInventory = state.Inventory with
        {
            OreCount = state.Inventory.OreCount - forged.OreCost,
            CrystalCount = state.Inventory.CrystalCount - forged.CrystalCost,
            ForgeScore = forged.ForgeScore,
        };
        int newPower = state.HeroRoster.HeroTeamPower + forged.PowerBonus;
        LobbyHeroRoster newRoster = state.HeroRoster with { HeroTeamPower = newPower };
        LobbyState nextState = state with
        {
            Inventory = newInventory,
            HeroRoster = newRoster,
            BattleReadyText = forged.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"锻造工坊复用 GameLogicService，锻造{forged.ItemName}评分 {forged.ForgeScore}，战力增加 {forged.PowerBonus}。"),
        };
        return MviReduceResult.State<LobbyState, LobbyEffect>(nextState);
    }

    private static LobbyHeroRoster ApplyHeroLevel(LobbyHeroRoster roster, HeroKind kind, int newLevel)
    {
        return kind switch
        {
            HeroKind.Warrior => roster with { WarriorLevel = newLevel },
            HeroKind.Mage => roster with { MageLevel = newLevel },
            HeroKind.Archer => roster with { ArcherLevel = newLevel },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "无效的英雄种类。"),
        };
    }

    private static int CalculateHeroPower(int warriorLevel, int mageLevel, int archerLevel)
    {
        return warriorLevel * 12 + mageLevel * 15 + archerLevel * 10;
    }

    private static LobbyState AppendActivityLog(LobbyState state, string message)
    {
        return state with { ActivityLog = AppendLogEntry(state.ActivityLog, message) };
    }

    private static string AppendLogEntry(string activityLog, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        string timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return string.Concat(activityLog, "[", timestamp, "] ", message, Environment.NewLine);
    }
}
