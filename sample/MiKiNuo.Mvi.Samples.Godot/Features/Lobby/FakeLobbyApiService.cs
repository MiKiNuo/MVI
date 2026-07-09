using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅后端 API 模拟实现。
/// </summary>
public sealed class FakeLobbyApiService : ILobbyApiService
{
    private readonly GameLogicService _gameLogicService;

    /// <summary>
    /// 初始化大厅后端 API 模拟实现。
    /// </summary>
    /// <param name="gameLogicService">游戏逻辑服务。</param>
    public FakeLobbyApiService(GameLogicService gameLogicService)
    {
        ArgumentNullException.ThrowIfNull(gameLogicService);
        _gameLogicService = gameLogicService;
    }

    /// <summary>
    /// 接受任务，模拟服务端校验与计算。
    /// </summary>
    public async ValueTask<AcceptMissionResult> AcceptMissionAsync(
        string missionName,
        int staminaCost,
        int baseReward,
        int heroPower,
        int currentStamina,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(missionName);
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        if (currentStamina < staminaCost)
        {
            return new AcceptMissionResult(
                Success: false,
                ErrorMessage: $"体力不足，无法接受 {missionName}。需要 {staminaCost} 点体力。",
                Reward: 0,
                NewStamina: currentStamina);
        }

        int reward = _gameLogicService.CalculateMissionReward(baseReward, heroPower);
        return new AcceptMissionResult(
            Success: true,
            ErrorMessage: null,
            Reward: reward,
            NewStamina: currentStamina - staminaCost);
    }

    /// <summary>
    /// 完成任务，模拟服务端奖励计算。
    /// </summary>
    public async ValueTask<int> CompleteMissionAsync(
        int heroPower,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        return _gameLogicService.CalculateMissionReward(60, heroPower);
    }

    /// <summary>
    /// 训练英雄，模拟服务端校验与计算。
    /// </summary>
    public async ValueTask<TrainHeroResult> TrainHeroAsync(
        string heroName,
        int currentLevel,
        int currentGold,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heroName);
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        int cost = _gameLogicService.CalculateTrainingCost(currentLevel);
        if (currentGold < cost)
        {
            return new TrainHeroResult(
                Success: false,
                ErrorMessage: $"金币不足，无法训练{heroName}。需要 {cost} 金币。",
                NewLevel: currentLevel,
                Cost: 0);
        }

        return new TrainHeroResult(
            Success: true,
            ErrorMessage: null,
            NewLevel: currentLevel + 1,
            Cost: cost);
    }

    /// <summary>
    /// 锻造装备，模拟服务端校验与计算。
    /// </summary>
    public async ValueTask<ForgeResult> ForgeAsync(
        string itemName,
        int oreCost,
        int crystalCost,
        int powerBonus,
        int heroPower,
        int oreCount,
        int crystalCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        if (oreCount < oreCost || crystalCount < crystalCost)
        {
            return new ForgeResult(
                Success: false,
                ErrorMessage: $"材料不足，无法锻造{itemName}。",
                ForgeScore: 0);
        }

        int forgeScore = _gameLogicService.CalculateForgeScore(heroPower, oreCount, crystalCount);
        return new ForgeResult(
            Success: true,
            ErrorMessage: null,
            ForgeScore: forgeScore);
    }

    /// <summary>
    /// 使用药水，模拟服务端校验与恢复。
    /// </summary>
    public async ValueTask<UsePotionResult> UsePotionAsync(
        int currentPotionCount,
        int currentStamina,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken).ConfigureAwait(false);

        if (currentPotionCount <= 0)
        {
            return new UsePotionResult(
                Success: false,
                ErrorMessage: "药水不足，不能恢复体力。",
                NewPotionCount: currentPotionCount,
                NewStamina: currentStamina);
        }

        return new UsePotionResult(
            Success: true,
            ErrorMessage: null,
            NewPotionCount: currentPotionCount - 1,
            NewStamina: Math.Min(100, currentStamina + 20));
    }

    /// <summary>
    /// 打开金币箱，模拟服务端奖励发放。
    /// </summary>
    public async ValueTask<int> OpenGoldBoxAsync(
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        return 120;
    }

    /// <summary>
    /// 生成战斗准备摘要，模拟服务端计算。
    /// </summary>
    public async ValueTask<string> BuildBattleReadyTextAsync(
        string selectedMission,
        int heroPower,
        int stamina,
        int potionCount,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        return _gameLogicService.BuildBattleReadyText(selectedMission, heroPower, stamina, potionCount);
    }
}
