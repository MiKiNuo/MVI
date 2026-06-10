using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 Reducer。
/// </summary>
public sealed partial class LobbyReducer : MviReducerBase<LobbyState, LobbyIntent, LobbyEffect>
{
    private readonly GameLogicService _gameLogicService;

    /// <summary>
    /// 初始化游戏大厅 Reducer。
    /// </summary>
    /// <param name="gameLogicService">共享游戏逻辑服务。</param>
    public LobbyReducer(GameLogicService gameLogicService)
    {
        _gameLogicService = gameLogicService ?? throw new ArgumentNullException(nameof(gameLogicService));
    }

    /// <summary>处理玩家资料传入意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SetPlayer intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        string readyText = _gameLogicService.BuildBattleReadyText(state.SelectedMission, state.HeroTeamPower, intent.Profile.Stamina, state.PotionCount);
        LobbyState nextState = state with
        {
            PlayerName = intent.Profile.PlayerName,
            PlayerLevel = intent.Profile.Level,
            Gold = intent.Profile.Gold,
            Stamina = intent.Profile.Stamina,
            CurrentPanel = LobbyPanelKeys.MissionBoard,
            CurrentPanelTitle = "任务大厅",
            MissionProgress = "登录成功，任务大厅等待指挥官选择任务。",
            BattleReadyText = readyText,
            ActivityLog = AppendLog(state.ActivityLog, $"Login MVI 传入玩家资料：{intent.Profile.PlayerName}。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace("Lobby SetPlayer"));
    }

    /// <summary>处理选择任务大厅意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SelectMissionBoard intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return SelectPanel(state, LobbyPanelKeys.MissionBoard, "任务大厅", "切换到任务大厅。不同子 MVI 会收到父状态变化。 ");
    }

    /// <summary>处理选择英雄队伍意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SelectHeroRoster intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return SelectPanel(state, LobbyPanelKeys.HeroRoster, "英雄队伍", "切换到英雄队伍，训练会影响战斗准备页。 ");
    }

    /// <summary>处理选择背包仓库意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SelectInventory intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return SelectPanel(state, LobbyPanelKeys.Inventory, "背包仓库", "切换到背包仓库，药水会影响战斗准备页。 ");
    }

    /// <summary>处理选择锻造工坊意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SelectForgeLab intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return SelectPanel(state, LobbyPanelKeys.ForgeLab, "锻造工坊", "切换到锻造工坊，用同一个 GameLogicService 验证逻辑复用。 ");
    }

    /// <summary>处理选择战斗准备意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.SelectBattlePrep intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return SelectPanel(state, LobbyPanelKeys.BattlePrep, "战斗准备", "切换到战斗准备，汇总任务、英雄、背包等子 MVI 数据。 ");
    }

    /// <summary>处理接受森林任务意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.AcceptForestMission intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return AcceptMission(state, "森林巡逻", staminaCost: 12, baseReward: 80);
    }

    /// <summary>处理接受矿洞任务意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.AcceptMineMission intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return AcceptMission(state, "矿洞救援", staminaCost: 18, baseReward: 125);
    }

    /// <summary>处理完成当前任务意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.CompleteMission intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        int reward = _gameLogicService.CalculateMissionReward(60, state.HeroTeamPower);
        LobbyState nextState = state with
        {
            Gold = state.Gold + reward,
            MissionProgress = $"{state.SelectedMission} 已完成，获得金币 {reward}。奖励由 GameLogicService 计算。",
            ActivityLog = AppendLog(state.ActivityLog, $"任务完成：{state.SelectedMission}，奖励 {reward}。"),
        };
        nextState = RefreshBattle(nextState);
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace($"Mission Complete reward={reward}"));
    }

    /// <summary>处理训练战士意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.TrainWarrior intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return TrainHero(state, "战士", state.WarriorLevel, level => state with { WarriorLevel = level });
    }

    /// <summary>处理训练法师意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.TrainMage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return TrainHero(state, "法师", state.MageLevel, level => state with { MageLevel = level });
    }

    /// <summary>处理训练弓手意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.TrainArcher intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return TrainHero(state, "弓手", state.ArcherLevel, level => state with { ArcherLevel = level });
    }

    /// <summary>处理使用药水意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.UsePotion intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        if (state.PotionCount <= 0)
        {
            return NoChange(state, "药水不足，不能恢复体力。");
        }

        LobbyState nextState = state with
        {
            PotionCount = state.PotionCount - 1,
            Stamina = Math.Min(100, state.Stamina + 20),
            ActivityLog = AppendLog(state.ActivityLog, "背包子 MVI 使用药水，体力恢复 20。"),
        };
        nextState = RefreshBattle(nextState);
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace("Inventory UsePotion"));
    }

    /// <summary>处理打开金币箱意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.OpenGoldBox intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        LobbyState nextState = state with
        {
            Gold = state.Gold + 120,
            ActivityLog = AppendLog(state.ActivityLog, "背包子 MVI 打开金币箱，金币增加 120。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace("Inventory OpenGoldBox"));
    }

    /// <summary>处理锻造武器意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.ForgeWeapon intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return Forge(state, "武器", oreCost: 2, crystalCost: 1, powerBonus: 8);
    }

    /// <summary>处理锻造护甲意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.ForgeArmor intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return Forge(state, "护甲", oreCost: 1, crystalCost: 1, powerBonus: 5);
    }

    /// <summary>处理准备战斗意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.PrepareBattle intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        LobbyState nextState = RefreshBattle(state) with
        {
            ActivityLog = AppendLog(state.ActivityLog, "战斗准备子 MVI 汇总任务、英雄、背包数据。"),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace("BattlePrep Prepare"));
    }

    /// <summary>处理退出登录意图。</summary>
    [MviReduce]
    private MviReduceResult<LobbyState, LobbyEffect> Reduce(LobbyState state, LobbyIntent.Logout intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        LobbyState nextState = state with
        {
            ActivityLog = AppendLog(state.ActivityLog, "大厅请求退出到登录页。"),
        };
        return MviReduceResult.StateAndEffects<LobbyState, LobbyEffect>(nextState, [new LobbyEffect.Trace("Lobby Logout"), new LobbyEffect.LogoutRequested()]);
    }

    private MviReduceResult<LobbyState, LobbyEffect> SelectPanel(LobbyState state, string panel, string title, string log)
    {
        LobbyState nextState = state with
        {
            CurrentPanel = panel,
            CurrentPanelTitle = title,
            ActivityLog = AppendLog(state.ActivityLog, log),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace($"Lobby Select {panel}"));
    }

    private MviReduceResult<LobbyState, LobbyEffect> AcceptMission(LobbyState state, string missionName, int staminaCost, int baseReward)
    {
        if (state.Stamina < staminaCost)
        {
            return NoChange(state, $"体力不足，无法接受 {missionName}。需要 {staminaCost} 点体力。 ");
        }

        int previewReward = _gameLogicService.CalculateMissionReward(baseReward, state.HeroTeamPower);
        LobbyState nextState = state with
        {
            SelectedMission = missionName,
            Stamina = state.Stamina - staminaCost,
            MissionProgress = $"已接受 {missionName}，消耗体力 {staminaCost}，预计奖励 {previewReward}。",
            ActivityLog = AppendLog(state.ActivityLog, $"任务子 MVI 接受 {missionName}，父 Lobby 状态同步体力和任务。"),
        };
        nextState = RefreshBattle(nextState);
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace($"Mission Accept {missionName}"));
    }

    private MviReduceResult<LobbyState, LobbyEffect> TrainHero(LobbyState state, string heroName, int currentLevel, Func<int, LobbyState> setLevel)
    {
        ArgumentNullException.ThrowIfNull(setLevel);
        int cost = _gameLogicService.CalculateTrainingCost(currentLevel);
        if (state.Gold < cost)
        {
            return NoChange(state, $"金币不足，无法训练{heroName}。需要 {cost} 金币。 ");
        }

        LobbyState leveledState = setLevel(currentLevel + 1);
        int nextPower = CalculateHeroPower(leveledState.WarriorLevel, leveledState.MageLevel, leveledState.ArcherLevel);
        LobbyState nextState = leveledState with
        {
            Gold = state.Gold - cost,
            HeroTeamPower = nextPower,
            ActivityLog = AppendLog(state.ActivityLog, $"英雄子 MVI 训练{heroName}，消耗 {cost} 金币，战力变为 {nextPower}。"),
        };
        nextState = RefreshBattle(nextState);
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace($"Hero Train {heroName}"));
    }

    private MviReduceResult<LobbyState, LobbyEffect> Forge(LobbyState state, string itemName, int oreCost, int crystalCost, int powerBonus)
    {
        if (state.OreCount < oreCost || state.CrystalCount < crystalCost)
        {
            return NoChange(state, $"材料不足，无法锻造{itemName}。 ");
        }

        int forgeScore = _gameLogicService.CalculateForgeScore(state.HeroTeamPower, state.OreCount, state.CrystalCount);
        LobbyState nextState = state with
        {
            OreCount = state.OreCount - oreCost,
            CrystalCount = state.CrystalCount - crystalCost,
            HeroTeamPower = state.HeroTeamPower + powerBonus,
            ForgeScore = forgeScore,
            ActivityLog = AppendLog(state.ActivityLog, $"锻造工坊复用 GameLogicService，锻造{itemName}评分 {forgeScore}，战力增加 {powerBonus}。"),
        };
        nextState = RefreshBattle(nextState);
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace($"Forge {itemName}"));
    }

    private MviReduceResult<LobbyState, LobbyEffect> NoChange(LobbyState state, string message)
    {
        LobbyState nextState = state with
        {
            ActivityLog = AppendLog(state.ActivityLog, message),
        };
        return MviReduceResult.StateAndEffect<LobbyState, LobbyEffect>(nextState, new LobbyEffect.Trace(message));
    }

    private LobbyState RefreshBattle(LobbyState state)
    {
        return state with
        {
            BattleReadyText = _gameLogicService.BuildBattleReadyText(state.SelectedMission, state.HeroTeamPower, state.Stamina, state.PotionCount),
        };
    }

    private static int CalculateHeroPower(int warriorLevel, int mageLevel, int archerLevel)
    {
        return warriorLevel * 12 + mageLevel * 15 + archerLevel * 10;
    }

    private static string AppendLog(string activityLog, string message)
    {
        ArgumentNullException.ThrowIfNull(activityLog);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        string timestamp = DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        return string.Concat(activityLog, "[", timestamp, "] ", message, Environment.NewLine);
    }
}
