using System;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录 MVI 状态。
/// </summary>
public sealed record LoginState : IMviState
{
    /// <summary>
    /// 初始化游戏登录 MVI 状态。
    /// </summary>
    /// <param name="userName">用户账号。</param>
    /// <param name="password">用户密码。</param>
    /// <param name="isBusy">是否正在登录。</param>
    /// <param name="errorMessage">错误消息。</param>
    /// <param name="canSubmit">是否允许提交。</param>
    /// <param name="loginStatus">登录状态说明。</param>
    public LoginState(string userName, string password, bool isBusy, string? errorMessage, bool canSubmit, string loginStatus)
    {
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(loginStatus);
        UserName = userName;
        Password = password;
        IsBusy = isBusy;
        ErrorMessage = errorMessage;
        CanSubmit = canSubmit;
        LoginStatus = loginStatus;
    }

    /// <summary>
    /// 获取用户账号。
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// 获取用户密码。
    /// </summary>
    public string Password { get; init; }

    /// <summary>
    /// 获取是否正在登录。
    /// </summary>
    public bool IsBusy { get; init; }

    /// <summary>
    /// 获取错误消息。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 获取是否允许提交。
    /// </summary>
    public bool CanSubmit { get; init; }

    /// <summary>
    /// 获取登录状态说明。
    /// </summary>
    public string LoginStatus { get; init; }

    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static LoginState Initial { get; } = new(
        userName: "miki",
        password: "123456",
        isBusy: false,
        errorMessage: null,
        canSubmit: true,
        loginStatus: "演示账号已预填。点击登录会进入游戏大厅。密码长度至少 3 位。");
}
