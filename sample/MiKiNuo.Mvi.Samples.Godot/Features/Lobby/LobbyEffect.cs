using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 MVI 副作用。
/// </summary>
public abstract partial record LobbyEffect : IMviEffect
{
    /// <summary>
    /// 表示写入大厅日志的副作用。
    /// </summary>
    public sealed partial record Trace : LobbyEffect
    {
        /// <summary>
        /// 初始化写入大厅日志副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>
        /// 获取日志文本。
        /// </summary>
        public string Text { get; init; }
    }

    /// <summary>
    /// 表示退出到登录页面的副作用。
    /// </summary>
    public sealed partial record LogoutRequested : LobbyEffect;

    /// <summary>
    /// 表示请求设置玩家资料的副作用。
    /// </summary>
    /// <param name="Profile">玩家资料。</param>
    public sealed partial record RequestSetPlayer(PlayerProfile Profile) : LobbyEffect;

    /// <summary>
    /// 表示请求接受任务的副作用。
    /// </summary>
    /// <param name="MissionName">任务名称。</param>
    /// <param name="StaminaCost">体力消耗。</param>
    /// <param name="BaseReward">基础奖励。</param>
    public sealed partial record RequestAcceptMission(
        string MissionName,
        int StaminaCost,
        int BaseReward) : LobbyEffect;

    /// <summary>
    /// 表示请求完成任务的副作用。
    /// </summary>
    public sealed partial record RequestCompleteMission : LobbyEffect;

    /// <summary>
    /// 表示请求训练英雄的副作用。
    /// </summary>
    /// <param name="Kind">英雄种类。</param>
    /// <param name="HeroName">英雄名称。</param>
    /// <param name="CurrentLevel">当前等级。</param>
    public sealed partial record RequestTrainHero(
        HeroKind Kind,
        string HeroName,
        int CurrentLevel) : LobbyEffect;

    /// <summary>
    /// 表示请求使用药水的副作用。
    /// </summary>
    public sealed partial record RequestUsePotion : LobbyEffect;

    /// <summary>
    /// 表示请求打开金币箱的副作用。
    /// </summary>
    public sealed partial record RequestOpenGoldBox : LobbyEffect;

    /// <summary>
    /// 表示请求锻造装备的副作用。
    /// </summary>
    /// <param name="ItemName">装备名称。</param>
    /// <param name="OreCost">矿石消耗。</param>
    /// <param name="CrystalCost">水晶消耗。</param>
    /// <param name="PowerBonus">战力加成。</param>
    public sealed partial record RequestForge(
        string ItemName,
        int OreCost,
        int CrystalCost,
        int PowerBonus) : LobbyEffect;

    /// <summary>
    /// 表示请求准备战斗的副作用。
    /// </summary>
    public sealed partial record RequestPrepareBattle : LobbyEffect;
}
