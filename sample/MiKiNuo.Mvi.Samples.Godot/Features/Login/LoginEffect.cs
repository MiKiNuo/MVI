using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录 MVI 副作用。
/// </summary>
public abstract partial record LoginEffect : IMviEffect
{
    /// <summary>
    /// 表示登录成功并进入大厅的副作用。
    /// </summary>
    public sealed partial record LoginSucceeded : LoginEffect
    {
        /// <summary>
        /// 初始化登录成功副作用。
        /// </summary>
        /// <param name="profile">玩家资料。</param>
        public LoginSucceeded(PlayerProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            Profile = profile;
        }

        /// <summary>
        /// 获取玩家资料。
        /// </summary>
        public PlayerProfile Profile { get; init; }
    }

    /// <summary>
    /// 表示写入登录日志的副作用。
    /// </summary>
    public sealed partial record Trace : LoginEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入登录日志副作用。
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
}
