using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏壳导航协调器，负责跨 Login MVI、Shell MVI 和 Player MVI 编排。
/// </summary>
public sealed class GameShellNavigator : IGameShellNavigator
{
    private readonly IMviStore<AppShellState, AppShellIntent, AppShellEffect> _appShellStore;
    private readonly IMviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;

    /// <summary>
    /// 初始化游戏壳导航协调器。
    /// </summary>
    /// <param name="appShellStore">应用壳状态存储。</param>
    /// <param name="playerStore">玩家资料状态存储。</param>
    public GameShellNavigator(
        IMviStore<AppShellState, AppShellIntent, AppShellEffect> appShellStore,
        IMviStore<PlayerState, PlayerIntent, PlayerEffect> playerStore)
    {
        ArgumentNullException.ThrowIfNull(appShellStore);
        _appShellStore = appShellStore;
        ArgumentNullException.ThrowIfNull(playerStore);
        _playerStore = playerStore;
    }

    /// <summary>
    /// 异步打开大厅并切换壳状态。
    /// </summary>
    /// <param name="profile">玩家档案。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async ValueTask OpenLobbyAsync(PlayerProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();
        await _playerStore.DispatchAsync(new PlayerIntent.SetPlayer(profile), cancellationToken).ConfigureAwait(false);
        await _appShellStore.DispatchAsync(new AppShellIntent.ShowLobby(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步返回登录界面。
    /// </summary>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步操作的任务。</returns>
    public ValueTask ReturnToLoginAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _appShellStore.DispatchAsync(new AppShellIntent.ShowLogin(), cancellationToken);
    }
}
