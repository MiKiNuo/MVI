using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅导航 MVI 副作用。
/// </summary>
public abstract partial record NavigationEffect : IMviEffect
{
    /// <summary>表示写入导航日志的副作用。</summary>
    public sealed partial record Trace : NavigationEffect, ITraceEffect
    {
        /// <summary>初始化导航日志副作用。</summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>获取日志文本。</summary>
        public string Text { get; init; }
    }

    /// <summary>表示记录导航活动的副作用。</summary>
    /// <param name="Message">活动消息。</param>
    public sealed partial record LogActivity(string Message) : NavigationEffect;

    /// <summary>表示请求退出登录的副作用。</summary>
    public sealed partial record LogoutRequested : NavigationEffect;
}
