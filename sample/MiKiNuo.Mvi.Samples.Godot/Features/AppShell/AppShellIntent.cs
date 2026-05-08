using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 意图。
/// </summary>
public abstract partial record AppShellIntent : IMviIntent
{
    /// <summary>
    /// 表示挂载顶层子 ViewModel 的意图。
    /// </summary>
    public sealed partial record AttachChildren : AppShellIntent
    {
        /// <summary>
        /// 初始化挂载顶层子 ViewModel 的意图。
        /// </summary>
        /// <param name="loginViewModel">登录页 ViewModel。</param>
        /// <param name="lobbyViewModel">游戏大厅 ViewModel。</param>
        public AttachChildren(LoginViewModel loginViewModel, LobbyViewModel lobbyViewModel)
        {
            ArgumentNullException.ThrowIfNull(loginViewModel);
            ArgumentNullException.ThrowIfNull(lobbyViewModel);
            LoginViewModel = loginViewModel;
            LobbyViewModel = lobbyViewModel;
        }

        /// <summary>
        /// 获取登录页 ViewModel。
        /// </summary>
        public LoginViewModel LoginViewModel { get; init; }

        /// <summary>
        /// 获取游戏大厅 ViewModel。
        /// </summary>
        public LobbyViewModel LobbyViewModel { get; init; }
    }

    /// <summary>
    /// 表示显示登录界面的意图。
    /// </summary>
    public sealed partial record ShowLogin : AppShellIntent;

    /// <summary>
    /// 表示显示游戏大厅的意图。
    /// </summary>
    public sealed partial record ShowLobby : AppShellIntent;
}
