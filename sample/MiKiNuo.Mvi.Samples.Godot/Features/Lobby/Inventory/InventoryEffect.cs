using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库 MVI 副作用。
/// </summary>
public abstract partial record InventoryEffect : IMviEffect
{
    /// <summary>
    /// 表示写入背包仓库日志的副作用。
    /// </summary>
    public sealed partial record Trace : InventoryEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入背包仓库日志副作用。
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

    /// <summary>表示恢复体力的副作用。</summary>
    /// <param name="NewStamina">恢复后体力。</param>
    public sealed partial record RestoreStamina(int NewStamina) : InventoryEffect;

    /// <summary>表示增加金币的副作用。</summary>
    /// <param name="Amount">金币数量。</param>
    public sealed partial record AddGold(int Amount) : InventoryEffect;

    /// <summary>表示更新战斗准备摘要的副作用。</summary>
    /// <param name="ReadyText">战斗准备摘要。</param>
    public sealed partial record UpdateBattleReadyText(string ReadyText) : InventoryEffect;

    /// <summary>表示记录活动日志的副作用。</summary>
    /// <param name="Message">日志消息。</param>
    public sealed partial record LogActivity(string Message) : InventoryEffect;
}
