using System;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 状态。
/// <para>
/// 不再持有 <c>LoginViewModel</c> / <c>LobbyViewModel</c> 引用；
/// 顶层页面 VM 通过 <see cref="IGameScreenFactory"/> 按 <see cref="CurrentScreen"/> 解析。
/// </para>
/// </summary>
public sealed record AppShellState : IMviState
{
    /// <summary>
    /// 初始化游戏应用壳 MVI 状态。
    /// </summary>
    /// <param name="currentScreen">当前顶层页面键。</param>
    /// <param name="currentTitle">当前页面标题。</param>
    /// <param name="shellMessage">应用壳提示消息。</param>
    public AppShellState(
        string currentScreen,
        string currentTitle,
        string shellMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentScreen);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(shellMessage);
        CurrentScreen = currentScreen;
        CurrentTitle = currentTitle;
        ShellMessage = shellMessage;
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
    /// 获取初始状态。
    /// </summary>
    public static AppShellState Initial { get; } = new(
        currentScreen: GameScreenKeys.Login,
        currentTitle: "登录游戏",
        shellMessage: "请输入账号和密码进入游戏大厅。");
}
