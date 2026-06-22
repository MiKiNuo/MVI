using System;
using System.Collections.Generic;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳顶层页面 ViewModel 的工厂实现。
/// <para>
/// 2 个顶层页面（Login / Lobby）由构造时一次性注入并缓存。
/// Login 与 Lobby 各自拥有独立的 store，缓存避免每次切换都重新构造。
/// </para>
/// </summary>
public sealed class GameScreenFactory : IGameScreenFactory
{
    private readonly IReadOnlyDictionary<string, object> _screenViewModels;

    /// <summary>
    /// 初始化游戏壳顶层页面 ViewModel 工厂。
    /// </summary>
    /// <param name="loginViewModel">登录页 ViewModel。</param>
    /// <param name="lobbyViewModel">游戏大厅 ViewModel。</param>
    public GameScreenFactory(LoginViewModel loginViewModel, LobbyViewModel lobbyViewModel)
    {
        ArgumentNullException.ThrowIfNull(loginViewModel);
        ArgumentNullException.ThrowIfNull(lobbyViewModel);

        _screenViewModels = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [GameScreenKeys.Login] = loginViewModel,
            [GameScreenKeys.Lobby] = lobbyViewModel,
        };
    }

    /// <summary>
    /// 根据页面键创建顶层页面 ViewModel。
    /// </summary>
    /// <param name="screenKey">页面键。</param>
    /// <returns>页面 ViewModel；未识别时返回 null。</returns>
    public object? CreateScreen(string screenKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenKey);
        return _screenViewModels.TryGetValue(screenKey, out object? viewModel) ? viewModel : null;
    }
}
