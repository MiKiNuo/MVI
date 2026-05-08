using System;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 状态。
/// </summary>
public sealed record AppShellState : IMviState
{
    /// <summary>
    /// 初始化游戏应用壳 MVI 状态。
    /// </summary>
    /// <param name="currentScreen">当前顶层页面键。</param>
    /// <param name="currentTitle">当前页面标题。</param>
    /// <param name="shellMessage">应用壳提示消息。</param>
    /// <param name="loginViewModel">登录页 ViewModel。</param>
    /// <param name="lobbyViewModel">游戏大厅 ViewModel。</param>
    public AppShellState(
        string currentScreen,
        string currentTitle,
        string shellMessage,
        LoginViewModel? loginViewModel,
        LobbyViewModel? lobbyViewModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentScreen);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(shellMessage);
        CurrentScreen = currentScreen;
        CurrentTitle = currentTitle;
        ShellMessage = shellMessage;
        LoginViewModel = loginViewModel;
        LobbyViewModel = lobbyViewModel;
    }

    /// <summary>
    /// 获取当前顶层页面键。
    /// </summary>
    public string CurrentScreen { get; init; }

    /// <summary>
    /// 获取当前页面标题。
    /// </summary>
    public string CurrentTitle { get; init; }

    /// <summary>
    /// 获取应用壳提示消息。
    /// </summary>
    public string ShellMessage { get; init; }

    /// <summary>
    /// 获取登录页 ViewModel。
    /// </summary>
    public LoginViewModel? LoginViewModel { get; init; }

    /// <summary>
    /// 获取游戏大厅 ViewModel。
    /// </summary>
    public LobbyViewModel? LobbyViewModel { get; init; }

    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static AppShellState Initial { get; } = new(
        currentScreen: GameScreenKeys.Login,
        currentTitle: "登录游戏",
        shellMessage: "请输入账号和密码进入游戏大厅。",
        loginViewModel: null,
        lobbyViewModel: null);
}
