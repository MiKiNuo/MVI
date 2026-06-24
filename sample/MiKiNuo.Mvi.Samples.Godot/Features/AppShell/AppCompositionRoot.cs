using System;
using System.Collections.Generic;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Composition;
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
/// <item><see cref="LobbyViewModel"/> 通过 <see cref="ILobbyChromeFactory"/> 解析 3 个常驻 chrome 子 VM（玩家头部 / 大厅菜单 / 活动日志），通过 <see cref="ILobbyPanelFactory"/> 解析 5 个互斥面板 VM。</item>
/// <item><see cref="AppShellViewModel"/> 通过 <see cref="IGameScreenFactory"/> 解析 Login / Lobby VM。</item>
/// </list>
/// <para>
/// 同时持有 <see cref="IGodotMviViewRegistry"/>（编译期生成）+ <see cref="GodotMviViewRegistryAdapter"/>
/// 桥接为 <see cref="IMviViewRegistry"/>，供 <see cref="global::MiKiNuo.Mvi.Samples.Godot.Views.Lobby.LobbyView"/> 等带 [MviSlot] 的 View 在源生成器
/// emit 的 <c>OnBindSlots</c> 钩子里按 <c>{Name}ViewModel</c> 解析为 <c>{Name}View</c>。
/// </para>
/// </summary>
public sealed class AppCompositionRoot : IDisposable, MiKiNuo.Mvi.Application.DI.IMviResolver
{
    private readonly MviMutationStore<AppShellState, AppShellIntent, AppShellMutation, AppShellEffect> _appShellStore;
    private readonly MviMutationStore<LoginState, LoginIntent, LoginMutation, LoginEffect> _loginStore;
    private readonly MviMutationStore<LobbyState, LobbyIntent, LobbyMutation, LobbyEffect> _lobbyStore;
    private readonly LoginViewModel _loginViewModel;
    private readonly LobbyViewModel _lobbyViewModel;
    private readonly GodotSampleGeneratedViewRegistry _godotViewRegistry;
    private readonly IMviViewRegistry _viewRegistry;
    private bool _disposed;

    /// <summary>
    /// 初始化 Godot 游戏示例应用组合根。
    /// </summary>
    /// <param name="uiDispatcher">Godot 主线程 UI 调度器（必填，确保 PropertyChanged/CanExecuteChanged marshal 到主线程）。</param>
    public AppCompositionRoot(IMviUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        GameLogicService gameLogicService = new();
        AppShellIntentHandler appShellIntentHandler = new();
        AppShellMutationReducer appShellMutationReducer = new();
        AppShellEffectDispatcher appShellEffectDispatcher = new();
        _appShellStore = new MviMutationStore<AppShellState, AppShellIntent, AppShellMutation, AppShellEffect>(
            AppShellState.Initial,
            appShellIntentHandler,
            appShellMutationReducer,
            appShellEffectDispatcher,
            Array.Empty<IMviMiddleware<AppShellState, AppShellIntent, AppShellEffect>>());

        LobbyMutationReducer lobbyMutationReducer = new();
        LobbyIntentHandler lobbyIntentHandler = new(new FakeLobbyApiService(gameLogicService));
        LobbyEffectDispatcher lobbyEffectDispatcher = new();
        IReadOnlyList<IMviMiddleware<LobbyState, LobbyIntent, LobbyEffect>> lobbyMiddlewares = [new LobbyMiddleware()];
        _lobbyStore = new MviMutationStore<LobbyState, LobbyIntent, LobbyMutation, LobbyEffect>(
            LobbyState.Initial,
            lobbyIntentHandler,
            lobbyMutationReducer,
            lobbyEffectDispatcher,
            lobbyMiddlewares);

        GameShellNavigator navigator = new(_appShellStore, _lobbyStore);
        lobbyEffectDispatcher.SetNavigator(navigator);
        LoginIntentHandler loginIntentHandler = new(new FakeAuthService());
        LoginMutationReducer loginMutationReducer = new();
        IReadOnlyList<IMviMiddleware<LoginState, LoginIntent, LoginEffect>> loginMiddlewares = [new LoginMiddleware()];
        _loginStore = new MviMutationStore<LoginState, LoginIntent, LoginMutation, LoginEffect>(
            LoginState.Initial,
            loginIntentHandler,
            loginMutationReducer,
            new LoginEffectDispatcher(navigator),
            loginMiddlewares);

        _loginViewModel = new LoginViewModel(_loginStore, uiDispatcher);

        // Lobby 8 个子 VM：3 个常驻 chrome（PlayerHeader / LobbyMenu / ActivityLog）+ 5 个互斥面板
        // 全部共用同一份 _lobbyStore，由组合根一次性构造：
        //   - 3 个常驻 chrome VM 交给 LobbyChromeFactory 工厂缓存
        //   - 5 个互斥面板 VM 交给 LobbyPanelFactory 工厂缓存
        PlayerHeaderViewModel playerHeaderViewModel = new(_lobbyStore, uiDispatcher);
        LobbyMenuViewModel lobbyMenuViewModel = new(_lobbyStore, uiDispatcher);
        ActivityLogViewModel activityLogViewModel = new(_lobbyStore, uiDispatcher);
        MissionBoardViewModel missionBoardViewModel = new(_lobbyStore, uiDispatcher);
        HeroRosterViewModel heroRosterViewModel = new(_lobbyStore, uiDispatcher);
        InventoryViewModel inventoryViewModel = new(_lobbyStore, uiDispatcher);
        ForgeLabViewModel forgeLabViewModel = new(_lobbyStore, uiDispatcher);
        BattlePrepViewModel battlePrepViewModel = new(_lobbyStore, uiDispatcher);
        ILobbyChromeFactory chromeFactory = new LobbyChromeFactory(
            playerHeaderViewModel,
            lobbyMenuViewModel,
            activityLogViewModel);
        ILobbyPanelFactory panelFactory = new LobbyPanelFactory(
            missionBoardViewModel,
            heroRosterViewModel,
            inventoryViewModel,
            forgeLabViewModel,
            battlePrepViewModel);
        _lobbyViewModel = new LobbyViewModel(
            _lobbyStore,
            chromeFactory,
            panelFactory,
            uiDispatcher);

        IGameScreenFactory screenFactory = new GameScreenFactory(_loginViewModel, _lobbyViewModel);
        AppShellViewModel = new AppShellViewModel(_appShellStore, screenFactory, uiDispatcher);

        // 编译期生成的 Godot ViewRegistry 通过 [MviGodotView] 注册所有 Lobby/PlayerHeader/MissionBoard 等子 View；
        // 用 GodotMviViewRegistryAdapter 桥接为平台无关 IMviViewRegistry，让源生成器在 [MviSlot] emit 的
        // OnBindSlots override 中通过 resolver.Resolve<IMviViewRegistry>() 即可按 {Name}ViewModel → {Name}View
        // 约定创建子 View。
        _godotViewRegistry = new GodotSampleGeneratedViewRegistry();
        _viewRegistry = new GodotMviViewRegistryAdapter(_godotViewRegistry);
    }

    /// <summary>
    /// 获取应用壳 ViewModel。
    /// </summary>
    public AppShellViewModel AppShellViewModel { get; }

    /// <summary>
    /// 获取平台无关 View 注册表（<see cref="GodotMviViewRegistryAdapter"/>），
    /// 由 <see cref="global::MiKiNuo.Mvi.Samples.Godot.Views.Lobby.LobbyView"/> 等带 [MviSlot] 字段的 View 在源生成器 emit 的 OnBindSlots 钩子中解析。
    /// </summary>
    public IMviViewRegistry ViewRegistry => _viewRegistry;

    /// <summary>
    /// 获取底层 Godot 平台 View 注册表（仅供组合根直接创建顶层 View 使用，
    /// 子 View 解析请通过 <see cref="ViewRegistry"/>）。
    /// </summary>
    public IGodotMviViewRegistry GodotViewRegistry => _godotViewRegistry;

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

    /// <summary>
    /// 解析 <typeparamref name="TService"/> 实例。
    /// <para>
    /// Godot 端组合根为手写装配，仅对源生成器期望解析的 <see cref="IMviViewRegistry"/>
    /// 返回本根持有的 <see cref="GodotMviViewRegistryAdapter"/>；其他类型保持显式访问属性
    /// （<see cref="AppShellViewModel"/> 等），不在本方法中暴露。
    /// </para>
    /// </summary>
    /// <typeparam name="TService">要解析的服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public TService Resolve<TService>()
        where TService : notnull
    {
        return (TService)Resolve(typeof(TService));
    }

    /// <summary>
    /// 按 <see cref="Type"/> 解析服务实例。
    /// </summary>
    /// <param name="serviceType">服务运行时类型。</param>
    /// <returns>服务实例。</returns>
    public object Resolve(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IMviViewRegistry))
        {
            return _viewRegistry;
        }

        throw new InvalidOperationException($"AppCompositionRoot 不解析此服务类型：{serviceType.FullName}。请改用对应属性直接访问。");
    }

    /// <summary>
    /// 按构造参数即时构造并返回服务实例。
    /// <para>
    /// Godot 端组合根为手写装配，本方法不实现；调用即抛出。
    /// </para>
    /// </summary>
    /// <typeparam name="TService">要实例化的服务类型。</typeparam>
    /// <param name="args">构造函数实参。</param>
    /// <returns>新构造的实例。</returns>
    public TService CreateWith<TService>(params object[] args)
        where TService : notnull
    {
        throw new NotSupportedException("AppCompositionRoot 是手写装配根，不支持 CreateWith<T>。");
    }

    /// <summary>
    /// 创建作用域。
    /// <para>
    /// Godot 端组合根为手写装配且所有服务为单例生命周期，返回一个空操作作用域。
    /// </para>
    /// </summary>
    /// <returns>服务作用域。</returns>
    public MiKiNuo.Mvi.Application.DI.IMviScope CreateScope()
    {
        return new EmptyScope();
    }

    /// <summary>
    /// 表示 Godot 端组合根的空操作作用域。
    /// </summary>
    private sealed class EmptyScope : MiKiNuo.Mvi.Application.DI.IMviScope
    {
        /// <summary>
        /// 解析作用域服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>服务实例。</returns>
        public TService Resolve<TService>()
            where TService : notnull
        {
            throw new NotSupportedException("Godot 端组合根作用域不解析服务，请直接访问 AppCompositionRoot 属性。");
        }

        /// <summary>
        /// 解析指定类型的作用域服务。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>服务实例。</returns>
        public object Resolve(Type serviceType)
        {
            throw new NotSupportedException("Godot 端组合根作用域不解析服务，请直接访问 AppCompositionRoot 属性。");
        }

        /// <summary>
        /// 按构造参数即时构造服务实例。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <param name="args">构造函数实参。</param>
        /// <returns>新构造的实例。</returns>
        public TService CreateWith<TService>(params object[] args)
            where TService : notnull
        {
            throw new NotSupportedException("Godot 端组合根作用域不支持 CreateWith<T>。");
        }

        /// <summary>
        /// 释放作用域资源。
        /// </summary>
        public void Dispose()
        {
        }
    }
}
