namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务规格。
/// </summary>
/// <param name="MissionName">任务名称。</param>
/// <param name="StaminaCost">体力消耗。</param>
/// <param name="BaseReward">基础奖励。</param>
public sealed record MissionSpec(
    string MissionName,
    int StaminaCost,
    int BaseReward)
{
    /// <summary>获取森林巡逻任务规格。</summary>
    public static MissionSpec ForestPatrol { get; } = new("森林巡逻", 12, 80);

    /// <summary>获取矿洞救援任务规格。</summary>
    public static MissionSpec MineRescue { get; } = new("矿洞救援", 18, 125);
}
