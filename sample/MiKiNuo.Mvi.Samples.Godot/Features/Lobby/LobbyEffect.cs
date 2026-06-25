using System;
using MiKiNuo.Mvi.Domain.MVI.Effect;

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
}
