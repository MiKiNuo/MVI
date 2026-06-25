using System;
using System.Globalization;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅规约器。
/// </summary>
public sealed partial class LobbyReducer
    : MviReducerBase<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>处理设置玩家资料意图。</summary>
    [MviReduce(typeof(LobbyIntent.SetPlayer))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSetPlayer(
        LobbyState state,
        LobbyIntent.SetPlayer intent)
    {
        PlayerProfile profile = intent.Profile;
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
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace("Lobby SetPlayer"));
    }

    /// <summary>处理选择任务大厅意图。</summary>
    [MviReduce(typeof(LobbyIntent.SelectMissionBoard))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSelectMissionBoard(
        LobbyState state,
        LobbyIntent.SelectMissionBoard intent)
    {
        return SelectPanel(state, LobbyPanelKeys.MissionBoard, "任务大厅", "切换到任务大厅。不同子 MVI 会收到父状态变化。 ", "Lobby Select MissionBoard");
    }

    /// <summary>处理选择英雄队伍意图。</summary>
    [MviReduce(typeof(LobbyIntent.SelectHeroRoster))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSelectHeroRoster(
        LobbyState state,
        LobbyIntent.SelectHeroRoster intent)
    {
        return SelectPanel(state, LobbyPanelKeys.HeroRoster, "英雄队伍", "切换到英雄队伍，训练会影响战斗准备页。 ", "Lobby Select HeroRoster");
    }

    /// <summary>处理选择背包仓库意图。</summary>
    [MviReduce(typeof(LobbyIntent.SelectInventory))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSelectInventory(
        LobbyState state,
        LobbyIntent.SelectInventory intent)
    {
        return SelectPanel(state, LobbyPanelKeys.Inventory, "背包仓库", "切换到背包仓库，药水会影响战斗准备页。 ", "Lobby Select Inventory");
    }

    /// <summary>处理选择锻造工坊意图。</summary>
    [MviReduce(typeof(LobbyIntent.SelectForgeLab))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSelectForgeLab(
        LobbyState state,
        LobbyIntent.SelectForgeLab intent)
    {
        return SelectPanel(state, LobbyPanelKeys.ForgeLab, "锻造工坊", "切换到锻造工坊，用同一个 GameLogicService 验证逻辑复用。 ", "Lobby Select ForgeLab");
    }

    /// <summary>处理选择战斗准备意图。</summary>
    [MviReduce(typeof(LobbyIntent.SelectBattlePrep))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleSelectBattlePrep(
        LobbyState state,
        LobbyIntent.SelectBattlePrep intent)
    {
        return SelectPanel(state, LobbyPanelKeys.BattlePrep, "战斗准备", "切换到战斗准备，汇总任务、英雄、背包等子 MVI 数据。 ", "Lobby Select BattlePrep");
    }

    /// <summary>处理玩家资料已设意图。</summary>
    [MviReduce(typeof(LobbyIntent.PlayerSet))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandlePlayerSet(
        LobbyState state,
        LobbyIntent.PlayerSet intent)
    {
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            state with { BattleReadyText = intent.BattleReadyText },
            new LobbyEffect.Trace("Lobby PlayerSet"));
    }

    /// <summary>处理任务已接受意图。</summary>
    [MviReduce(typeof(LobbyIntent.MissionAccepted))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleMissionAccepted(
        LobbyState state,
        LobbyIntent.MissionAccepted intent)
    {
        LobbyMission newMission = new(
            intent.MissionName,
            $"已接受 {intent.MissionName}，消耗体力 {intent.StaminaCost}，预计奖励 {intent.Reward}。");
        LobbyState nextState = state with
        {
            Player = state.Player with { Stamina = intent.NewStamina },
            Mission = newMission,
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"任务子 MVI 接受 {intent.MissionName}，父 Lobby 状态同步体力和任务。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace($"Mission Accept {intent.MissionName}"));
    }

    /// <summary>处理任务接受失败意图。</summary>
    [MviReduce(typeof(LobbyIntent.MissionAcceptFailed))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleMissionAcceptFailed(
        LobbyState state,
        LobbyIntent.MissionAcceptFailed intent)
    {
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            AppendActivityLog(state, intent.ErrorMessage ?? "接受任务失败。"),
            new LobbyEffect.Trace("Mission Accept Failed"));
    }

    /// <summary>处理任务已完成意图。</summary>
    [MviReduce(typeof(LobbyIntent.MissionCompleted))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleMissionCompleted(
        LobbyState state,
        LobbyIntent.MissionCompleted intent)
    {
        LobbyMission newMission = state.Mission with
        {
            MissionProgress = $"{state.Mission.SelectedMission} 已完成，获得金币 {intent.Reward}。奖励由 GameLogicService 计算。",
        };
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold + intent.Reward },
            Mission = newMission,
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"任务完成：{state.Mission.SelectedMission}，奖励 {intent.Reward}。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace($"Mission Complete reward={intent.Reward}"));
    }

    /// <summary>处理英雄训练成功意图。</summary>
    [MviReduce(typeof(LobbyIntent.HeroTrained))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleHeroTrained(
        LobbyState state,
        LobbyIntent.HeroTrained intent)
    {
        LobbyHeroRoster leveledRoster = ApplyHeroLevel(state.HeroRoster, intent.Kind, intent.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        LobbyHeroRoster newRoster = leveledRoster with { HeroTeamPower = nextPower };
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold - intent.Cost },
            HeroRoster = newRoster,
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"英雄子 MVI 训练{intent.HeroName}，消耗 {intent.Cost} 金币，战力变为 {nextPower}。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace($"Hero Train {intent.HeroName}"));
    }

    /// <summary>处理英雄训练失败意图。</summary>
    [MviReduce(typeof(LobbyIntent.HeroTrainFailed))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleHeroTrainFailed(
        LobbyState state,
        LobbyIntent.HeroTrainFailed intent)
    {
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            AppendActivityLog(state, intent.ErrorMessage ?? "训练失败。"),
            new LobbyEffect.Trace("Hero Train Failed"));
    }

    /// <summary>处理药水使用成功意图。</summary>
    [MviReduce(typeof(LobbyIntent.PotionUsed))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandlePotionUsed(
        LobbyState state,
        LobbyIntent.PotionUsed intent)
    {
        LobbyInventory newInventory = state.Inventory with { PotionCount = intent.NewPotionCount };
        LobbyState nextState = state with
        {
            Inventory = newInventory,
            Player = state.Player with { Stamina = intent.NewStamina },
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, "背包子 MVI 使用药水，体力恢复 20。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace("Inventory UsePotion"));
    }

    /// <summary>处理药水使用失败意图。</summary>
    [MviReduce(typeof(LobbyIntent.PotionUseFailed))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandlePotionUseFailed(
        LobbyState state,
        LobbyIntent.PotionUseFailed intent)
    {
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            AppendActivityLog(state, intent.ErrorMessage ?? "使用药水失败。"),
            new LobbyEffect.Trace("Inventory UsePotion Failed"));
    }

    /// <summary>处理金币箱已打开意图。</summary>
    [MviReduce(typeof(LobbyIntent.GoldBoxOpened))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleGoldBoxOpened(
        LobbyState state,
        LobbyIntent.GoldBoxOpened intent)
    {
        LobbyState nextState = state with
        {
            Player = state.Player with { Gold = state.Player.Gold + intent.Gold },
            ActivityLog = AppendLogEntry(state.ActivityLog, $"背包子 MVI 打开金币箱，金币增加 {intent.Gold}。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace("Inventory OpenGoldBox"));
    }

    /// <summary>处理锻造成功意图。</summary>
    [MviReduce(typeof(LobbyIntent.Forged))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleForged(
        LobbyState state,
        LobbyIntent.Forged intent)
    {
        LobbyInventory newInventory = state.Inventory with
        {
            OreCount = state.Inventory.OreCount - intent.OreCost,
            CrystalCount = state.Inventory.CrystalCount - intent.CrystalCost,
            ForgeScore = intent.ForgeScore,
        };
        int newPower = state.HeroRoster.HeroTeamPower + intent.PowerBonus;
        LobbyHeroRoster newRoster = state.HeroRoster with { HeroTeamPower = newPower };
        LobbyState nextState = state with
        {
            Inventory = newInventory,
            HeroRoster = newRoster,
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, $"锻造工坊复用 GameLogicService，锻造{intent.ItemName}评分 {intent.ForgeScore}，战力增加 {intent.PowerBonus}。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace($"Forge {intent.ItemName}"));
    }

    /// <summary>处理锻造失败意图。</summary>
    [MviReduce(typeof(LobbyIntent.ForgeFailed))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleForgeFailed(
        LobbyState state,
        LobbyIntent.ForgeFailed intent)
    {
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            AppendActivityLog(state, intent.ErrorMessage ?? "锻造失败。"),
            new LobbyEffect.Trace("Forge Failed"));
    }

    /// <summary>处理战斗准备完成意图。</summary>
    [MviReduce(typeof(LobbyIntent.BattlePrepared))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleBattlePrepared(
        LobbyState state,
        LobbyIntent.BattlePrepared intent)
    {
        LobbyState nextState = state with
        {
            BattleReadyText = intent.BattleReadyText,
            ActivityLog = AppendLogEntry(state.ActivityLog, "战斗准备子 MVI 汇总任务、英雄、背包数据。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace("BattlePrep Prepare"));
    }

    /// <summary>处理退出登录意图。</summary>
    [MviReduce(typeof(LobbyIntent.Logout))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleLogout(
        LobbyState state,
        LobbyIntent.Logout intent)
    {
        LobbyState nextState = AppendActivityLog(state, "大厅请求退出到登录页。");
        return MviReduceResult.StateAndEffects<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Logout"),
                new LobbyEffect.LogoutRequested(),
            });
    }

    /// <summary>处理接受森林任务请求。</summary>
    [MviReduce(typeof(LobbyIntent.AcceptForestMission))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleAcceptForestMission(
        LobbyState state,
        LobbyIntent.AcceptForestMission intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理接受矿洞任务请求。</summary>
    [MviReduce(typeof(LobbyIntent.AcceptMineMission))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleAcceptMineMission(
        LobbyState state,
        LobbyIntent.AcceptMineMission intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理完成任务请求。</summary>
    [MviReduce(typeof(LobbyIntent.CompleteMission))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleCompleteMission(
        LobbyState state,
        LobbyIntent.CompleteMission intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理训练战士请求。</summary>
    [MviReduce(typeof(LobbyIntent.TrainWarrior))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleTrainWarrior(
        LobbyState state,
        LobbyIntent.TrainWarrior intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理训练法师请求。</summary>
    [MviReduce(typeof(LobbyIntent.TrainMage))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleTrainMage(
        LobbyState state,
        LobbyIntent.TrainMage intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理训练弓箭手请求。</summary>
    [MviReduce(typeof(LobbyIntent.TrainArcher))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleTrainArcher(
        LobbyState state,
        LobbyIntent.TrainArcher intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理使用药水请求。</summary>
    [MviReduce(typeof(LobbyIntent.UsePotion))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleUsePotion(
        LobbyState state,
        LobbyIntent.UsePotion intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理打开金币箱请求。</summary>
    [MviReduce(typeof(LobbyIntent.OpenGoldBox))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleOpenGoldBox(
        LobbyState state,
        LobbyIntent.OpenGoldBox intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理锻造武器请求。</summary>
    [MviReduce(typeof(LobbyIntent.ForgeWeapon))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleForgeWeapon(
        LobbyState state,
        LobbyIntent.ForgeWeapon intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理锻造护甲请求。</summary>
    [MviReduce(typeof(LobbyIntent.ForgeArmor))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandleForgeArmor(
        LobbyState state,
        LobbyIntent.ForgeArmor intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    /// <summary>处理战斗准备请求。</summary>
    [MviReduce(typeof(LobbyIntent.PrepareBattle))]
    private static MviReduceResult<LobbyState, LobbyEffect> HandlePrepareBattle(
        LobbyState state,
        LobbyIntent.PrepareBattle intent)
        => MviReduceResult.State<LobbyState, LobbyEffect>(state);

    private static MviReduceResult<LobbyState, LobbyEffect> SelectPanel(
        LobbyState state,
        string panel,
        string title,
        string log,
        string traceText)
    {
        LobbyNavigation newNavigation = new(panel, title);
        LobbyState nextState = state with
        {
            Navigation = newNavigation,
            ActivityLog = AppendLogEntry(state.ActivityLog, log),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(
            nextState,
            new LobbyEffect.Trace(traceText));
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
