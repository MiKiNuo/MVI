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
    /// 表示请求登录副作用。
    /// </summary>
    public sealed partial record RequestLogin : LoginEffect
    {
        /// <summary>
        /// 初始化请求登录副作用。
        /// </summary>
        /// <param name="userName">用户账号。</param>
        /// <param name="password">用户密码。</param>
        public RequestLogin(string userName, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// 获取用户账号。
        /// </summary>
        public string UserName { get; init; }

        /// <summary>
        /// 获取用户密码。
        /// </summary>
        public string Password { get; init; }
    }

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
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>
        /// 获取玩家资料。
        /// </summary>
        public PlayerProfile Profile { get; init; }
    }

    /// <summary>
    /// 表示写入登录日志的副作用。
    /// </summary>
    public sealed partial record Trace : LoginEffect
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
