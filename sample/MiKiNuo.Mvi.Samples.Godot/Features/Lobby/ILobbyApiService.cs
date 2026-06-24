using System.Threading;
using System.Threading.Tasks;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅后端 API 服务接口。
/// </summary>
public interface ILobbyApiService
{
    /// <summary>
    /// 接受任务，服务端校验体力并计算奖励。
    /// </summary>
    /// <param name="missionName">任务名称。</param>
    /// <param name="staminaCost">体力消耗。</param>
    /// <param name="baseReward">基础奖励。</param>
    /// <param name="heroPower">英雄战力。</param>
    /// <param name="currentStamina">当前体力。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>接受任务结果。</returns>
    public ValueTask<AcceptMissionResult> AcceptMissionAsync(
        string missionName,
        int staminaCost,
        int baseReward,
        int heroPower,
        int currentStamina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成当前任务，服务端计算奖励。
    /// </summary>
    /// <param name="heroPower">英雄战力。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>完成任务奖励。</returns>
    public ValueTask<int> CompleteMissionAsync(
        int heroPower,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 训练英雄，服务端校验金币并计算消耗。
    /// </summary>
    /// <param name="heroName">英雄名称。</param>
    /// <param name="currentLevel">当前等级。</param>
    /// <param name="currentGold">当前金币。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>训练结果。</returns>
    public ValueTask<TrainHeroResult> TrainHeroAsync(
        string heroName,
        int currentLevel,
        int currentGold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 锻造装备，服务端校验材料并计算评分。
    /// </summary>
    /// <param name="itemName">装备名称。</param>
    /// <param name="oreCost">矿石消耗。</param>
    /// <param name="crystalCost">水晶消耗。</param>
    /// <param name="powerBonus">战力加成。</param>
    /// <param name="heroPower">英雄战力。</param>
    /// <param name="oreCount">当前矿石。</param>
    /// <param name="crystalCount">当前水晶。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>锻造结果。</returns>
    public ValueTask<ForgeResult> ForgeAsync(
        string itemName,
        int oreCost,
        int crystalCost,
        int powerBonus,
        int heroPower,
        int oreCount,
        int crystalCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用药水恢复体力。
    /// </summary>
    /// <param name="currentPotionCount">当前药水数。</param>
    /// <param name="currentStamina">当前体力。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>使用药水结果。</returns>
    public ValueTask<UsePotionResult> UsePotionAsync(
        int currentPotionCount,
        int currentStamina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开金币箱获取奖励。
    /// </summary>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>获得金币数。</returns>
    public ValueTask<int> OpenGoldBoxAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成战斗准备摘要文本。
    /// </summary>
    /// <param name="selectedMission">当前任务。</param>
    /// <param name="heroPower">英雄战力。</param>
    /// <param name="stamina">体力值。</param>
    /// <param name="potionCount">药水数。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>战斗准备摘要。</returns>
    public ValueTask<string> BuildBattleReadyTextAsync(
        string selectedMission,
        int heroPower,
        int stamina,
        int potionCount,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示接受任务结果。
/// </summary>
/// <param name="Success">是否成功。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="Reward">预计奖励。</param>
/// <param name="NewStamina">剩余体力。</param>
public sealed record AcceptMissionResult(
    bool Success,
    string? ErrorMessage,
    int Reward,
    int NewStamina);

/// <summary>
/// 表示训练英雄结果。
/// </summary>
/// <param name="Success">是否成功。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="NewLevel">新等级。</param>
/// <param name="Cost">消耗金币。</param>
public sealed record TrainHeroResult(
    bool Success,
    string? ErrorMessage,
    int NewLevel,
    int Cost);

/// <summary>
/// 表示锻造装备结果。
/// </summary>
/// <param name="Success">是否成功。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="ForgeScore">锻造评分。</param>
public sealed record ForgeResult(
    bool Success,
    string? ErrorMessage,
    int ForgeScore);

/// <summary>
/// 表示使用药水结果。
/// </summary>
/// <param name="Success">是否成功。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="NewPotionCount">剩余药水。</param>
/// <param name="NewStamina">恢复后体力。</param>
public sealed record UsePotionResult(
    bool Success,
    string? ErrorMessage,
    int NewPotionCount,
    int NewStamina);
