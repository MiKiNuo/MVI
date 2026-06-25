using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 MVI 意图。
/// </summary>
public abstract partial record LobbyIntent : IMviIntent
{
    /// <summary>
    /// 表示设置玩家资料的意图。
    /// </summary>
    public sealed partial record SetPlayer : LobbyIntent
    {
        /// <summary>
        /// 初始化设置玩家资料的意图。
        /// </summary>
        /// <param name="profile">玩家资料。</param>
        public SetPlayer(PlayerProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>
        /// 获取玩家资料。
        /// </summary>
        public PlayerProfile Profile { get; init; }
    }

    /// <summary>表示选择任务大厅面板的意图。</summary>
    public sealed partial record SelectMissionBoard : LobbyIntent;

    /// <summary>表示选择英雄队伍面板的意图。</summary>
    public sealed partial record SelectHeroRoster : LobbyIntent;

    /// <summary>表示选择背包仓库面板的意图。</summary>
    public sealed partial record SelectInventory : LobbyIntent;

    /// <summary>表示选择锻造工坊面板的意图。</summary>
    public sealed partial record SelectForgeLab : LobbyIntent;

    /// <summary>表示选择战斗准备面板的意图。</summary>
    public sealed partial record SelectBattlePrep : LobbyIntent;

    /// <summary>表示接受森林任务的意图。</summary>
    public sealed partial record AcceptForestMission : LobbyIntent;

    /// <summary>表示接受矿洞任务的意图。</summary>
    public sealed partial record AcceptMineMission : LobbyIntent;

    /// <summary>表示完成当前任务的意图。</summary>
    public sealed partial record CompleteMission : LobbyIntent;

    /// <summary>表示训练战士的意图。</summary>
    public sealed partial record TrainWarrior : LobbyIntent;

    /// <summary>表示训练法师的意图。</summary>
    public sealed partial record TrainMage : LobbyIntent;

    /// <summary>表示训练弓手的意图。</summary>
    public sealed partial record TrainArcher : LobbyIntent;

    /// <summary>表示使用药水的意图。</summary>
    public sealed partial record UsePotion : LobbyIntent;

    /// <summary>表示打开金币箱的意图。</summary>
    public sealed partial record OpenGoldBox : LobbyIntent;

    /// <summary>表示锻造武器的意图。</summary>
    public sealed partial record ForgeWeapon : LobbyIntent;

    /// <summary>表示锻造护甲的意图。</summary>
    public sealed partial record ForgeArmor : LobbyIntent;

    /// <summary>表示准备战斗的意图。</summary>
    public sealed partial record PrepareBattle : LobbyIntent;

    /// <summary>表示退出到登录页的意图。</summary>
    public sealed partial record Logout : LobbyIntent;

    /// <summary>
    /// 表示玩家资料已设置的意图。
    /// </summary>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record PlayerSet(string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示任务已接受的意图。
    /// </summary>
    /// <param name="MissionName">任务名称。</param>
    /// <param name="StaminaCost">体力消耗。</param>
    /// <param name="Reward">预计奖励。</param>
    /// <param name="NewStamina">剩余体力。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record MissionAccepted(
        string MissionName,
        int StaminaCost,
        int Reward,
        int NewStamina,
        string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示任务接受失败的意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record MissionAcceptFailed(string ErrorMessage) : LobbyIntent;

    /// <summary>
    /// 表示任务已完成的意图。
    /// </summary>
    /// <param name="Reward">奖励金币。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record MissionCompleted(int Reward, string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示英雄训练成功的意图。
    /// </summary>
    /// <param name="Kind">英雄种类。</param>
    /// <param name="HeroName">英雄名称。</param>
    /// <param name="NewLevel">新等级。</param>
    /// <param name="Cost">消耗金币。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record HeroTrained(
        HeroKind Kind,
        string HeroName,
        int NewLevel,
        int Cost,
        string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示英雄训练失败的意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record HeroTrainFailed(string ErrorMessage) : LobbyIntent;

    /// <summary>
    /// 表示药水使用成功的意图。
    /// </summary>
    /// <param name="NewPotionCount">剩余药水。</param>
    /// <param name="NewStamina">恢复后体力。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record PotionUsed(
        int NewPotionCount,
        int NewStamina,
        string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示药水使用失败的意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record PotionUseFailed(string ErrorMessage) : LobbyIntent;

    /// <summary>
    /// 表示金币箱已打开的意图。
    /// </summary>
    /// <param name="Gold">获得金币。</param>
    public sealed partial record GoldBoxOpened(int Gold) : LobbyIntent;

    /// <summary>
    /// 表示锻造成功的意图。
    /// </summary>
    /// <param name="ItemName">装备名称。</param>
    /// <param name="OreCost">矿石消耗。</param>
    /// <param name="CrystalCost">水晶消耗。</param>
    /// <param name="PowerBonus">战力加成。</param>
    /// <param name="ForgeScore">锻造评分。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record Forged(
        string ItemName,
        int OreCost,
        int CrystalCost,
        int PowerBonus,
        int ForgeScore,
        string BattleReadyText) : LobbyIntent;

    /// <summary>
    /// 表示锻造失败的意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record ForgeFailed(string ErrorMessage) : LobbyIntent;

    /// <summary>
    /// 表示战斗准备完成的意图。
    /// </summary>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record BattlePrepared(string BattleReadyText) : LobbyIntent;
}
