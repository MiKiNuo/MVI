using System;
using System.Collections.Generic;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示 Godot 游戏示例应用组合根。
/// <para>
/// 父 VM 不再持有子 VM 引用：
/// </para>
/// <list type="bullet">
/// <item><see cref="LobbyViewModel"/> 通过 <see cref="ILobbyPanelFactory"/> 解析 5 个互斥面板 VM，3 个常驻 VM 由构造函数注入。</item>
/// <item><see cref="AppShellViewModel"/> 通过 <see cref="IGameScreenFactory"/> 解析 Login / Lobby VM。</item>
/// </list>
/// </summary>
public sealed class AppCompositionRoot : IDisposable
{
    private readonly MviStore<AppShellState, AppShellIntent, AppShellEffect> _appShellStore;
    private readonly MviStore<LoginState, LoginIntent, LoginEffect> _loginStore;
    private readonly MviStore<LobbyState, LobbyIntent, LobbyEffect> _lobbyStore;
    private readonly LoginViewModel _loginViewModel;
    private readonly LobbyViewModel _lobbyViewModel;
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

        // Lobby 7 个子 VM：3 个常驻（PlayerHeader / LobbyMenu / ActivityLog）+ 5 个互斥面板
        // 全部共用同一份 _lobbyStore，由组合根一次性构造并交给工厂缓存。
        PlayerHeaderViewModel playerHeaderViewModel = new(_lobbyStore, uiDispatcher);
        LobbyMenuViewModel lobbyMenuViewModel = new(_lobbyStore, uiDispatcher);
        ActivityLogViewModel activityLogViewModel = new(_lobbyStore, uiDispatcher);
        MissionBoardViewModel missionBoardViewModel = new(_lobbyStore, uiDispatcher);
        HeroRosterViewModel heroRosterViewModel = new(_lobbyStore, uiDispatcher);
        InventoryViewModel inventoryViewModel = new(_lobbyStore, uiDispatcher);
        ForgeLabViewModel forgeLabViewModel = new(_lobbyStore, uiDispatcher);
        BattlePrepViewModel battlePrepViewModel = new(_lobbyStore, uiDispatcher);
        ILobbyPanelFactory panelFactory = new LobbyPanelFactory(
            missionBoardViewModel,
            heroRosterViewModel,
            inventoryViewModel,
            forgeLabViewModel,
            battlePrepViewModel);
        _lobbyViewModel = new LobbyViewModel(
            _lobbyStore,
            playerHeaderViewModel,
            lobbyMenuViewModel,
            activityLogViewModel,
            panelFactory,
            uiDispatcher);

        IGameScreenFactory screenFactory = new GameScreenFactory(_loginViewModel, _lobbyViewModel);
        AppShellViewModel = new AppShellViewModel(_appShellStore, screenFactory, uiDispatcher);
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
        _lobbyViewModel.Dispose();
        _loginViewModel.Dispose();
        _loginStore.Dispose();
        _lobbyStore.Dispose();
        _appShellStore.Dispose();
        _disposed = true;
    }
}
