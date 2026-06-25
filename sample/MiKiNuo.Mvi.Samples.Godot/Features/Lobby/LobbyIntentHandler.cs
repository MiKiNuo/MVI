using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅意图处理器。
/// </summary>
public sealed class LobbyIntentHandler
    : IMviIntentHandler<LobbyState, LobbyIntent, LobbyEffect>
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
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<LobbyIntent>> HandleAsync(
        LobbyState state,
        LobbyIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        switch (intent)
        {
            case LobbyIntent.SetPlayer setPlayer:
                return await HandleSetPlayerAsync(state, setPlayer, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.AcceptForestMission:
                return await HandleAcceptMissionAsync(state, "森林巡逻", 12, 80, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.AcceptMineMission:
                return await HandleAcceptMissionAsync(state, "矿洞救援", 18, 125, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.CompleteMission:
                return await HandleCompleteMissionAsync(state, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.TrainWarrior:
                return await HandleTrainHeroAsync(state, HeroKind.Warrior, "战士", state.HeroRoster.WarriorLevel, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.TrainMage:
                return await HandleTrainHeroAsync(state, HeroKind.Mage, "法师", state.HeroRoster.MageLevel, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.TrainArcher:
                return await HandleTrainHeroAsync(state, HeroKind.Archer, "弓手", state.HeroRoster.ArcherLevel, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.UsePotion:
                return await HandleUsePotionAsync(state, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.OpenGoldBox:
                return await HandleOpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
            case LobbyIntent.ForgeWeapon:
                return await HandleForgeAsync(state, "武器", 2, 1, 8, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.ForgeArmor:
                return await HandleForgeAsync(state, "护甲", 1, 1, 5, cancellationToken).ConfigureAwait(false);
            case LobbyIntent.PrepareBattle:
                return await HandlePrepareBattleAsync(state, cancellationToken).ConfigureAwait(false);
            default:
                return Array.Empty<LobbyIntent>();
        }
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleSetPlayerAsync(
        LobbyState state,
        LobbyIntent.SetPlayer intent,
        CancellationToken cancellationToken)
    {
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                state.HeroRoster.HeroTeamPower,
                intent.Profile.Stamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[] { new LobbyIntent.PlayerSet(readyText) };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleAcceptMissionAsync(
        LobbyState state,
        string missionName,
        int staminaCost,
        int baseReward,
        CancellationToken cancellationToken)
    {
        AcceptMissionResult result = await _apiService
            .AcceptMissionAsync(
                missionName,
                staminaCost,
                baseReward,
                state.HeroRoster.HeroTeamPower,
                state.Player.Stamina,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new LobbyIntent[]
            {
                new LobbyIntent.MissionAcceptFailed(result.ErrorMessage ?? "接受任务失败。"),
            };
        }

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                missionName,
                state.HeroRoster.HeroTeamPower,
                result.NewStamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[]
        {
            new LobbyIntent.MissionAccepted(missionName, staminaCost, result.Reward, result.NewStamina, readyText),
        };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleCompleteMissionAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        int reward = await _apiService
            .CompleteMissionAsync(state.HeroRoster.HeroTeamPower, cancellationToken)
            .ConfigureAwait(false);
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                state.HeroRoster.HeroTeamPower,
                state.Player.Stamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[] { new LobbyIntent.MissionCompleted(reward, readyText) };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleTrainHeroAsync(
        LobbyState state,
        HeroKind kind,
        string heroName,
        int currentLevel,
        CancellationToken cancellationToken)
    {
        TrainHeroResult result = await _apiService
            .TrainHeroAsync(heroName, currentLevel, state.Player.Gold, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new LobbyIntent[]
            {
                new LobbyIntent.HeroTrainFailed(result.ErrorMessage ?? "训练失败。"),
            };
        }

        LobbyHeroRoster leveledRoster = ApplyHeroLevel(state.HeroRoster, kind, result.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                nextPower,
                state.Player.Stamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[]
        {
            new LobbyIntent.HeroTrained(kind, heroName, result.NewLevel, result.Cost, readyText),
        };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleUsePotionAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        UsePotionResult result = await _apiService
            .UsePotionAsync(state.Inventory.PotionCount, state.Player.Stamina, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new LobbyIntent[]
            {
                new LobbyIntent.PotionUseFailed(result.ErrorMessage ?? "使用药水失败。"),
            };
        }

        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                state.HeroRoster.HeroTeamPower,
                result.NewStamina,
                result.NewPotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[]
        {
            new LobbyIntent.PotionUsed(result.NewPotionCount, result.NewStamina, readyText),
        };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleOpenGoldBoxAsync(CancellationToken cancellationToken)
    {
        int gold = await _apiService.OpenGoldBoxAsync(cancellationToken).ConfigureAwait(false);
        return new LobbyIntent[] { new LobbyIntent.GoldBoxOpened(gold) };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandleForgeAsync(
        LobbyState state,
        string itemName,
        int oreCost,
        int crystalCost,
        int powerBonus,
        CancellationToken cancellationToken)
    {
        ForgeResult result = await _apiService
            .ForgeAsync(
                itemName,
                oreCost,
                crystalCost,
                powerBonus,
                state.HeroRoster.HeroTeamPower,
                state.Inventory.OreCount,
                state.Inventory.CrystalCount,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return new LobbyIntent[]
            {
                new LobbyIntent.ForgeFailed(result.ErrorMessage ?? "锻造失败。"),
            };
        }

        int newPower = state.HeroRoster.HeroTeamPower + powerBonus;
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                newPower,
                state.Player.Stamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[]
        {
            new LobbyIntent.Forged(itemName, oreCost, crystalCost, powerBonus, result.ForgeScore, readyText),
        };
    }

    private async ValueTask<IReadOnlyList<LobbyIntent>> HandlePrepareBattleAsync(
        LobbyState state,
        CancellationToken cancellationToken)
    {
        string readyText = await _apiService
            .BuildBattleReadyTextAsync(
                state.Mission.SelectedMission,
                state.HeroRoster.HeroTeamPower,
                state.Player.Stamina,
                state.Inventory.PotionCount,
                cancellationToken)
            .ConfigureAwait(false);
        return new LobbyIntent[] { new LobbyIntent.BattlePrepared(readyText) };
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
}
