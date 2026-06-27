using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料 MVI 意图。
/// </summary>
public abstract partial record PlayerIntent : IMviIntent
{
    /// <summary>表示设置玩家资料的意图。</summary>
    public sealed partial record SetPlayer : PlayerIntent
    {
        /// <summary>初始化设置玩家资料意图。</summary>
        /// <param name="profile">玩家资料。</param>
        public SetPlayer(PlayerProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>获取玩家资料。</summary>
        public PlayerProfile Profile { get; init; }
    }

    /// <summary>表示玩家资料已设置的意图。</summary>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record PlayerSet(string BattleReadyText) : PlayerIntent;

    /// <summary>表示消耗金币的意图。</summary>
    /// <param name="Amount">消耗数量。</param>
    public sealed partial record ConsumeGold(int Amount) : PlayerIntent;

    /// <summary>表示增加金币的意图。</summary>
    /// <param name="Amount">增加数量。</param>
    public sealed partial record AddGold(int Amount) : PlayerIntent;

    /// <summary>表示消耗体力的意图。</summary>
    /// <param name="Amount">消耗数量。</param>
    public sealed partial record ConsumeStamina(int Amount) : PlayerIntent;

    /// <summary>表示恢复体力的意图。</summary>
    /// <param name="NewStamina">恢复后体力。</param>
    public sealed partial record RestoreStamina(int NewStamina) : PlayerIntent;
}
