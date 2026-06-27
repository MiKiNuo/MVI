using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志副作用。
/// </summary>
public abstract partial record ActivityLogEffect : IMviEffect
{
    /// <summary>
    /// 表示写入活动日志追踪的副作用。
    /// </summary>
    public sealed partial record Trace : ActivityLogEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入活动日志追踪副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>获取日志文本。</summary>
        public string Text { get; init; }
    }
}
