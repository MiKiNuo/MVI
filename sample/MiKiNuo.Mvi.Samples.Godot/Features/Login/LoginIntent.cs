using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录 MVI 意图。
/// </summary>
public abstract partial record LoginIntent : IMviIntent
{
    /// <summary>
    /// 表示修改账号的意图。
    /// </summary>
    public sealed partial record ChangeUserName : LoginIntent
    {
        /// <summary>
        /// 初始化修改账号意图。
        /// </summary>
        /// <param name="userName">用户账号。</param>
        public ChangeUserName(string userName)
        {
            ArgumentNullException.ThrowIfNull(userName);
            UserName = userName;
        }

        /// <summary>
        /// 获取用户账号。
        /// </summary>
        public string UserName { get; init; }
    }

    /// <summary>
    /// 表示修改密码的意图。
    /// </summary>
    public sealed partial record ChangePassword : LoginIntent
    {
        /// <summary>
        /// 初始化修改密码意图。
        /// </summary>
        /// <param name="password">用户密码。</param>
        public ChangePassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            Password = password;
        }

        /// <summary>
        /// 获取用户密码。
        /// </summary>
        public string Password { get; init; }
    }

    /// <summary>
    /// 表示提交登录的意图。
    /// </summary>
    public sealed partial record Submit : LoginIntent;

    /// <summary>
    /// 表示登录成功意图。
    /// </summary>
    public sealed partial record LoginSucceeded : LoginIntent
    {
        /// <summary>
        /// 初始化登录成功意图。
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
    /// 表示登录失败意图。
    /// </summary>
    public sealed partial record LoginFailed : LoginIntent
    {
        /// <summary>
        /// 初始化登录失败意图。
        /// </summary>
        /// <param name="errorMessage">错误消息。</param>
        public LoginFailed(string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 获取错误消息。
        /// </summary>
        public string ErrorMessage { get; init; }
    }
}
