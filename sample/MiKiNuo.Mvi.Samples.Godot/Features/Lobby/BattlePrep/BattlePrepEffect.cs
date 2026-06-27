using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备 MVI 副作用。
/// </summary>
public abstract partial record BattlePrepEffect : IMviEffect
{
    /// <summary>
    /// 表示写入追踪日志的副作用。
    /// </summary>
    public sealed partial record Trace : BattlePrepEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入追踪日志副作用。
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
    /// 表示追加活动日志的副作用。
    /// </summary>
    /// <param name="Message">日志消息。</param>
    public sealed partial record LogActivity(string Message) : BattlePrepEffect;
}
