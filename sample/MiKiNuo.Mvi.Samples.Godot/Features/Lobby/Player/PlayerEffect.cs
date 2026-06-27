using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料 MVI 副作用。
/// </summary>
public abstract partial record PlayerEffect : IMviEffect
{
    /// <summary>表示写入玩家日志的副作用。</summary>
    public sealed partial record Trace : PlayerEffect, ITraceEffect
    {
        /// <summary>初始化玩家日志副作用。</summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>获取日志文本。</summary>
        public string Text { get; init; }
    }

    /// <summary>表示更新战斗准备摘要的副作用。</summary>
    /// <param name="ReadyText">战斗准备摘要。</param>
    public sealed partial record UpdateBattleReadyText(string ReadyText) : PlayerEffect;

    /// <summary>表示记录玩家活动的副作用。</summary>
    /// <param name="Message">活动消息。</param>
    public sealed partial record LogActivity(string Message) : PlayerEffect;
}
