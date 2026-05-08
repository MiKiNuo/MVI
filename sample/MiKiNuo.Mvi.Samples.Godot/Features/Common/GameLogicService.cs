using System;
using System.Globalization;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Common;

/// <summary>
/// 表示登录、任务、英雄训练和锻造工坊共同复用的游戏业务逻辑。
/// </summary>
public sealed class GameLogicService
{
    /// <summary>
    /// 根据账号创建玩家资料。
    /// </summary>
    /// <param name="account">登录账号。</param>
    /// <returns>玩家资料。</returns>
    public PlayerProfile CreateProfile(string account)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        string playerName = string.Create(CultureInfo.InvariantCulture, $"{account.Trim()} 指挥官");
        return new PlayerProfile(playerName, level: 12, gold: 360, stamina: 80);
    }

    /// <summary>
    /// 计算任务奖励金币。
    /// </summary>
    /// <param name="baseReward">任务基础奖励。</param>
    /// <param name="heroPower">英雄队伍战力。</param>
    /// <returns>最终奖励金币。</returns>
    public int CalculateMissionReward(int baseReward, int heroPower)
    {
        return Math.Max(0, baseReward + heroPower / 12);
    }

    /// <summary>
    /// 计算英雄训练消耗金币。
    /// </summary>
    /// <param name="currentLevel">英雄当前等级。</param>
    /// <returns>训练消耗金币。</returns>
    public int CalculateTrainingCost(int currentLevel)
    {
        return 30 + Math.Max(0, currentLevel) * 8;
    }

    /// <summary>
    /// 计算锻造评分。
    /// </summary>
    /// <param name="heroPower">英雄队伍战力。</param>
    /// <param name="oreCount">矿石数量。</param>
    /// <param name="crystalCount">水晶数量。</param>
    /// <returns>锻造评分。</returns>
    public int CalculateForgeScore(int heroPower, int oreCount, int crystalCount)
    {
        return Math.Max(0, heroPower / 2 + oreCount * 9 + crystalCount * 18);
    }

    /// <summary>
    /// 生成战斗准备摘要。
    /// </summary>
    /// <param name="selectedMission">当前选中的任务。</param>
    /// <param name="heroPower">英雄队伍战力。</param>
    /// <param name="stamina">体力值。</param>
    /// <param name="potionCount">药水数量。</param>
    /// <returns>战斗准备摘要。</returns>
    public string BuildBattleReadyText(string selectedMission, int heroPower, int stamina, int potionCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedMission);
        bool ready = heroPower >= 45 && stamina >= 15;
        string status = ready ? "可以出征" : "暂不建议出征";
        return $"任务：{selectedMission}；战力：{heroPower}；体力：{stamina}；药水：{potionCount}；结论：{status}。";
    }
}
