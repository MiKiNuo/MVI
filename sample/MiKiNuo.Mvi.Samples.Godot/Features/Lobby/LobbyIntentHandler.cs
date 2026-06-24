using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅意图处理器。
/// </summary>
public sealed class LobbyIntentHandler
    : IMviIntentHandler<LobbyState, LobbyIntent, LobbyMutation, LobbyEffect>
{
    private readonly ILobbyApiService _apiService;

    /// <summary>
    /// 初始化游戏大厅意图处理器。
    /// </summary>
    /// <param name="apiService">大厅后端 API 服务。</param>
    public LobbyIntentHandler(ILobbyApiService apiService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
    }

    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> HandleAsync(
        LobbyState state,
        LobbyIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            LobbyIntent.SetPlayer setPlayer => await HandleSetPlayerAsync(state, setPlayer, cancellationToken).ConfigureAwait(false),
            LobbyIntent.SelectMissionBoard => SelectPanel(state, LobbyPanelKeys.MissionBoard, "任务大厅", "切换到任务大厅。不同子 MVI 会收到父状态变化。 "),
            LobbyIntent.SelectHeroRoster => SelectPanel(state, LobbyPanelKeys.HeroRoster, "英雄队伍", "切换到英雄队伍，训练会影响战斗准备页。 "),
            LobbyIntent.SelectInventory => SelectPanel(state, LobbyPanelKeys.Inventory, "背包仓库", "切换到背包仓库，药水会影响战斗准备页。 "),
            LobbyIntent.SelectForgeLab => SelectPanel(state, LobbyPanelKeys.ForgeLab, "锻造工坊", "切换到锻造工坊，用同一个 GameLogicService 验证逻辑复用。 "),
            LobbyIntent.SelectBattlePrep => SelectPanel(state, LobbyPanelKeys.BattlePrep, "战斗准备", "切换到战斗准备，汇总任务、英雄、背包等子 MVI 数据。 "),
            LobbyIntent.AcceptForestMission => await AcceptMissionAsync(state, "森林巡逻", staminaCost: 12, baseReward: 80, cancellationToken).ConfigureAwait(false),
            LobbyIntent.AcceptMineMission => await AcceptMissionAsync(state, "矿洞救援", staminaCost: 18, baseReward: 125, cancellationToken).ConfigureAwait(false),
            LobbyIntent.CompleteMission => await CompleteMissionAsync(state, cancellationToken).ConfigureAwait(false),
            LobbyIntent.TrainWarrior => await TrainHeroAsync(state, "战士", state.HeroRoster.WarriorLevel, (roster, level) => roster with { WarriorLevel = level }, cancellationToken).ConfigureAwait(false),
            LobbyIntent.TrainMage => await TrainHeroAsync(state, "法师", state.HeroRoster.MageLevel, (roster, level) => roster with { MageLevel = level }, cancellationToken).ConfigureAwait(false),
            LobbyIntent.TrainArcher => await TrainHeroAsync(state, "弓手", state.HeroRoster.ArcherLevel, (roster, level) => roster with { ArcherLevel = level }, cancellationToken).ConfigureAwait(false),
            LobbyIntent.UsePotion => await UsePotionAsync(state, cancellationToken).ConfigureAwait(false),
            LobbyIntent.OpenGoldBox => await OpenGoldBoxAsync(state, cancellationToken).ConfigureAwait(false),
            LobbyIntent.ForgeWeapon => await ForgeAsync(state, "武器", oreCost: 2, crystalCost: 1, powerBonus: 8, cancellationToken).ConfigureAwait(false),
            LobbyIntent.ForgeArmor => await ForgeAsync(state, "护甲", oreCost: 1, crystalCost: 1, powerBonus: 5, cancellationToken).ConfigureAwait(false),
            LobbyIntent.PrepareBattle => await PrepareBattleAsync(state, cancellationToken).ConfigureAwait(false),
            LobbyIntent.Logout => HandleLogout(state),
            _ => MviHandleResult.Empty<LobbyMutation, LobbyEffect>(),
        };
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> HandleSetPlayerAsync(
        LobbyState state,
        LobbyIntent.SetPlayer intent,
        CancellationToken cancellationToken)
    {
        PlayerProfile profile = intent.Profile;
        LobbyPlayer newPlayer = new(profile.PlayerName, profile.Level, profile.Gold, profile.Stamina);
        LobbyNavigation newNavigation = new(LobbyPanelKeys.MissionBoard, "任务大厅");
        LobbyMission newMission = state.Mission with
        {
            MissionProgress = "登录成功，任务大厅等待指挥官选择任务。",
        };
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(newMission.SelectedMission, state.HeroRoster.HeroTeamPower, profile.Stamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetPlayer(newPlayer),
                new LobbyMutation.SetNavigation(newNavigation),
                new LobbyMutation.SetMission(newMission),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity($"Login MVI 传入玩家资料：{profile.PlayerName}。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace("Lobby SetPlayer") });
    }

    private static MviHandleResult<LobbyMutation, LobbyEffect> SelectPanel(
        LobbyState state,
        string panel,
        string title,
        string log)
    {
        LobbyNavigation newNavigation = new(panel, title);
        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetNavigation(newNavigation),
                LogActivity(log),
            },
            new LobbyEffect[] { new LobbyEffect.Trace($"Lobby Select {panel}") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> AcceptMissionAsync(
        LobbyState state,
        string missionName,
        int staminaCost,
        int baseReward,
        CancellationToken cancellationToken)
    {
        AcceptMissionResult result = await _apiService
            .AcceptMissionAsync(missionName, staminaCost, baseReward, state.HeroRoster.HeroTeamPower, state.Player.Stamina, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
                new LobbyMutation[]
                {
                    LogActivity(result.ErrorMessage ?? "接受任务失败。"),
                },
                new LobbyEffect[] { new LobbyEffect.Trace($"Mission Accept Failed {missionName}") });
        }

        LobbyMission newMission = new(missionName, $"已接受 {missionName}，消耗体力 {staminaCost}，预计奖励 {result.Reward}。");
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(newMission.SelectedMission, state.HeroRoster.HeroTeamPower, result.NewStamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetPlayerStamina(result.NewStamina),
                new LobbyMutation.SetMission(newMission),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity($"任务子 MVI 接受 {missionName}，父 Lobby 状态同步体力和任务。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace($"Mission Accept {missionName}") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> CompleteMissionAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        int reward = await _apiService
            .CompleteMissionAsync(state.HeroRoster.HeroTeamPower, cancellationToken)
            .ConfigureAwait(false);

        LobbyMission newMission = state.Mission with
        {
            MissionProgress = $"{state.Mission.SelectedMission} 已完成，获得金币 {reward}。奖励由 GameLogicService 计算。",
        };
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(newMission.SelectedMission, state.HeroRoster.HeroTeamPower, state.Player.Stamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.AddPlayerGold(reward),
                new LobbyMutation.SetMission(newMission),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity($"任务完成：{state.Mission.SelectedMission}，奖励 {reward}。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace($"Mission Complete reward={reward}") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> TrainHeroAsync(
        LobbyState state,
        string heroName,
        int currentLevel,
        Func<LobbyHeroRoster, int, LobbyHeroRoster> setLevel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setLevel);
        TrainHeroResult result = await _apiService
            .TrainHeroAsync(heroName, currentLevel, state.Player.Gold, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
                new LobbyMutation[]
                {
                    LogActivity(result.ErrorMessage ?? "训练失败。"),
                },
                new LobbyEffect[] { new LobbyEffect.Trace($"Hero Train Failed {heroName}") });
        }

        LobbyHeroRoster leveledRoster = setLevel(state.HeroRoster, result.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        LobbyHeroRoster newRoster = leveledRoster with { HeroTeamPower = nextPower };
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(state.Mission.SelectedMission, nextPower, state.Player.Stamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.AddPlayerGold(-result.Cost),
                new LobbyMutation.SetHeroRoster(newRoster),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity($"英雄子 MVI 训练{heroName}，消耗 {result.Cost} 金币，战力变为 {nextPower}。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace($"Hero Train {heroName}") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> UsePotionAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        UsePotionResult result = await _apiService
            .UsePotionAsync(state.Inventory.PotionCount, state.Player.Stamina, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
                new LobbyMutation[]
                {
                    LogActivity(result.ErrorMessage ?? "使用药水失败。"),
                },
                new LobbyEffect[] { new LobbyEffect.Trace("Inventory UsePotion Failed") });
        }

        LobbyInventory newInventory = state.Inventory with { PotionCount = result.NewPotionCount };
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(state.Mission.SelectedMission, state.HeroRoster.HeroTeamPower, result.NewStamina, result.NewPotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetInventory(newInventory),
                new LobbyMutation.SetPlayerStamina(result.NewStamina),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity("背包子 MVI 使用药水，体力恢复 20。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace("Inventory UsePotion") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> OpenGoldBoxAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        int gold = await _apiService
            .OpenGoldBoxAsync(cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.AddPlayerGold(gold),
                LogActivity($"背包子 MVI 打开金币箱，金币增加 {gold}。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace("Inventory OpenGoldBox") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> ForgeAsync(
        LobbyState state,
        string itemName,
        int oreCost,
        int crystalCost,
        int powerBonus,
        CancellationToken cancellationToken)
    {
        ForgeResult result = await _apiService
            .ForgeAsync(itemName, oreCost, crystalCost, powerBonus, state.HeroRoster.HeroTeamPower, state.Inventory.OreCount, state.Inventory.CrystalCount, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
                new LobbyMutation[]
                {
                    LogActivity(result.ErrorMessage ?? "锻造失败。"),
                },
                new LobbyEffect[] { new LobbyEffect.Trace($"Forge Failed {itemName}") });
        }

        LobbyInventory newInventory = state.Inventory with
        {
            OreCount = state.Inventory.OreCount - oreCost,
            CrystalCount = state.Inventory.CrystalCount - crystalCost,
            ForgeScore = result.ForgeScore,
        };
        int newPower = state.HeroRoster.HeroTeamPower + powerBonus;
        LobbyHeroRoster newRoster = state.HeroRoster with { HeroTeamPower = newPower };
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(state.Mission.SelectedMission, newPower, state.Player.Stamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetInventory(newInventory),
                new LobbyMutation.SetHeroRoster(newRoster),
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity($"锻造工坊复用 GameLogicService，锻造{itemName}评分 {result.ForgeScore}，战力增加 {powerBonus}。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace($"Forge {itemName}") });
    }

    private async ValueTask<MviHandleResult<LobbyMutation, LobbyEffect>> PrepareBattleAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(state.Mission.SelectedMission, state.HeroRoster.HeroTeamPower, state.Player.Stamina, state.Inventory.PotionCount, cancellationToken)
            .ConfigureAwait(false);

        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                new LobbyMutation.SetBattleReadyText(readyText),
                LogActivity("战斗准备子 MVI 汇总任务、英雄、背包数据。"),
            },
            new LobbyEffect[] { new LobbyEffect.Trace("BattlePrep Prepare") });
    }

    private static MviHandleResult<LobbyMutation, LobbyEffect> HandleLogout(LobbyState state)
    {
        return MviHandleResult.MutationsAndEffects<LobbyMutation, LobbyEffect>(
            new LobbyMutation[]
            {
                LogActivity("大厅请求退出到登录页。"),
            },
            new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Logout"),
                new LobbyEffect.LogoutRequested(),
            });
    }

    private static int CalculateHeroPower(int warriorLevel, int mageLevel, int archerLevel)
    {
        return warriorLevel * 12 + mageLevel * 15 + archerLevel * 10;
    }

    private static string FormatLog(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        string timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return string.Concat("[", timestamp, "] ", message, Environment.NewLine);
    }

    private static LobbyMutation LogActivity(string message)
    {
        return LogActivity(FormatLog(message));
    }
}
