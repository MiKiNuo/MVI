using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示 Godot 游戏示例应用组合根。
/// </summary>
public sealed class AppCompositionRoot : IDisposable
{
    private readonly MviStore<AppShellState, AppShellIntent, AppShellEffect> _appShellStore;
    private readonly MviStore<LoginState, LoginIntent, LoginEffect> _loginStore;
    private readonly MviStore<LobbyState, LobbyIntent, LobbyEffect> _lobbyStore;
    private readonly LoginViewModel _loginViewModel;
    private readonly LobbyViewModel _lobbyViewModel;
    private readonly PlayerHeaderViewModel _playerHeaderViewModel;
    private readonly LobbyMenuViewModel _lobbyMenuViewModel;
    private readonly MissionBoardViewModel _missionBoardViewModel;
    private readonly HeroRosterViewModel _heroRosterViewModel;
    private readonly InventoryViewModel _inventoryViewModel;
    private readonly ForgeLabViewModel _forgeLabViewModel;
    private readonly BattlePrepViewModel _battlePrepViewModel;
    private readonly ActivityLogViewModel _activityLogViewModel;
    private bool _disposed;

    /// <summary>
    /// 初始化 Godot 游戏示例应用组合根。
    /// </summary>
    /// <param name="uiDispatcher">Godot 主线程 UI 调度器（必填，确保 PropertyChanged/CanExecuteChanged marshal 到主线程）。</param>
    public AppCompositionRoot(IMviUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        GameLogicService gameLogicService = new();
        AppShellReducer appShellReducer = new();
        AppShellEffectDispatcher appShellEffectDispatcher = new();
        _appShellStore = new MviStore<AppShellState, AppShellIntent, AppShellEffect>(
            AppShellState.Initial,
            appShellReducer,
            appShellEffectDispatcher,
            Array.Empty<IMviMiddleware<AppShellState, AppShellIntent, AppShellEffect>>());

        LobbyReducer lobbyReducer = new(gameLogicService);
        LobbyEffectDispatcher lobbyEffectDispatcher = new();
        IReadOnlyList<IMviMiddleware<LobbyState, LobbyIntent, LobbyEffect>> lobbyMiddlewares = [new LobbyMiddleware()];
        _lobbyStore = new MviStore<LobbyState, LobbyIntent, LobbyEffect>(
            LobbyState.Initial,
            lobbyReducer,
            lobbyEffectDispatcher,
            lobbyMiddlewares);

        GameShellNavigator navigator = new(_appShellStore, _lobbyStore);
        lobbyEffectDispatcher.SetNavigator(navigator);
        LoginReducer loginReducer = new(gameLogicService);
        IReadOnlyList<IMviMiddleware<LoginState, LoginIntent, LoginEffect>> loginMiddlewares = [new LoginMiddleware()];
        _loginStore = new MviStore<LoginState, LoginIntent, LoginEffect>(
            LoginState.Initial,
            loginReducer,
            new LoginEffectDispatcher(navigator),
            loginMiddlewares);

        _loginViewModel = new LoginViewModel(_loginStore, uiDispatcher);
        _lobbyViewModel = new LobbyViewModel(_lobbyStore, uiDispatcher);
        _playerHeaderViewModel = new PlayerHeaderViewModel(_lobbyStore, uiDispatcher);
        _lobbyMenuViewModel = new LobbyMenuViewModel(_lobbyStore, uiDispatcher);
        _missionBoardViewModel = new MissionBoardViewModel(_lobbyStore, uiDispatcher);
        _heroRosterViewModel = new HeroRosterViewModel(_lobbyStore, uiDispatcher);
        _inventoryViewModel = new InventoryViewModel(_lobbyStore, uiDispatcher);
        _forgeLabViewModel = new ForgeLabViewModel(_lobbyStore, uiDispatcher);
        _battlePrepViewModel = new BattlePrepViewModel(_lobbyStore, uiDispatcher);
        _activityLogViewModel = new ActivityLogViewModel(_lobbyStore, uiDispatcher);

        Wait(_lobbyStore.DispatchAsync(new LobbyIntent.AttachChildren(
            _playerHeaderViewModel,
            _lobbyMenuViewModel,
            _missionBoardViewModel,
            _heroRosterViewModel,
            _inventoryViewModel,
            _forgeLabViewModel,
            _battlePrepViewModel,
            _activityLogViewModel)));

        Wait(_appShellStore.DispatchAsync(new AppShellIntent.AttachChildren(_loginViewModel, _lobbyViewModel)));

        AppShellViewModel = new AppShellViewModel(_appShellStore, uiDispatcher);
    }

    /// <summary>
    /// 获取应用壳 ViewModel。
    /// </summary>
    public AppShellViewModel AppShellViewModel { get; }

    /// <summary>
    /// 释放应用组合根。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        AppShellViewModel.Dispose();
        _activityLogViewModel.Dispose();
        _battlePrepViewModel.Dispose();
        _forgeLabViewModel.Dispose();
        _inventoryViewModel.Dispose();
        _heroRosterViewModel.Dispose();
        _missionBoardViewModel.Dispose();
        _lobbyMenuViewModel.Dispose();
        _playerHeaderViewModel.Dispose();
        _lobbyViewModel.Dispose();
        _loginViewModel.Dispose();
        _loginStore.Dispose();
        _lobbyStore.Dispose();
        _appShellStore.Dispose();
        _disposed = true;
    }

    private static void Wait(ValueTask task)
    {
        if (!task.IsCompletedSuccessfully)
        {
            task.AsTask().GetAwaiter().GetResult();
        }
    }
}
