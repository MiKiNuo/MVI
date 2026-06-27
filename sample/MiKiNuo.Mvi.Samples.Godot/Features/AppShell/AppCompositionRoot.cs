using System;
using System.Collections.Generic;
using global::Godot;
using R3;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Tracing;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using MiKiNuo.Mvi.Samples.Godot.Features.Login;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示 Godot 游戏示例应用组合根。
/// <para>
/// 大厅拆分为 8 个独立 Store：ActivityLog / Navigation / BattlePrep / Player / Mission / HeroRoster / Inventory / ForgeLab。
/// 各 Store 拥有完整 MVI 模型（State/Intent/Effect/Reducer/IntentHandler/EffectDispatcher）。
/// </para>
/// <para>
/// IntentHandler 与 EffectDispatcher 之间存在循环依赖（Player↔Mission↔HeroRoster↔Inventory 互相读取 CurrentState，
/// 并向 BattlePrep 写入 DispatchAsync）。通过 <see cref="StoreReference{TState,TIntent,TEffect}"/> 代理模式打破：
/// 先创建引用占位，构造完真实 Store 后通过 <c>SetTarget</c> 注入。
/// </para>
/// <para>
/// 同时持有 <see cref="IGodotMviViewRegistry"/>（编译期生成）+ <see cref="GodotMviViewRegistryAdapter"/>
/// 桥接为 <see cref="IMviViewRegistry"/>，供带 [MviSlot] 的 View 在源生成器 emit 的 <c>OnBindSlots</c> 钩子里按 <c>{Name}ViewModel</c> 解析为 <c>{Name}View</c>。
/// </para>
/// </summary>
public sealed class AppCompositionRoot : IDisposable, MiKiNuo.Mvi.Application.DI.IMviResolver
{
    private readonly MviStore<AppShellState, AppShellIntent, AppShellEffect> _appShellStore;
    private readonly MviStore<LoginState, LoginIntent, LoginEffect> _loginStore;
    private readonly MviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> _activityLogStore;
    private readonly MviStore<NavigationState, NavigationIntent, NavigationEffect> _navigationStore;
    private readonly MviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect> _battlePrepStore;
    private readonly MviStore<PlayerState, PlayerIntent, PlayerEffect> _playerStore;
    private readonly MviStore<MissionState, MissionIntent, MissionEffect> _missionStore;
    private readonly MviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect> _heroRosterStore;
    private readonly MviStore<InventoryState, InventoryIntent, InventoryEffect> _inventoryStore;
    private readonly MviStore<UnitState, ForgeLabIntent, ForgeLabEffect> _forgeLabStore;
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

        GodotTraceEffectLogger traceLogger = new GodotTraceEffectLogger();
        GameLogicService gameLogicService = new();
        FakeLobbyApiService apiService = new(gameLogicService);

        // === AppShell Store ===
        AppShellIntentHandler appShellIntentHandler = new();
        AppShellReducer appShellReducer = new();
        AppShellEffectDispatcher appShellEffectDispatcher = new(traceLogger);
        _appShellStore = new MviStore<AppShellState, AppShellIntent, AppShellEffect>(
            AppShellState.Initial,
            appShellIntentHandler,
            appShellReducer,
            appShellEffectDispatcher,
            Array.Empty<IMviMiddleware<AppShellState, AppShellIntent, AppShellEffect>>());

        // === 1. ActivityLogStore（叶子，无兄弟依赖） ===
        ActivityLogIntentHandler activityLogIntentHandler = new();
        ActivityLogReducer activityLogReducer = new();
        ActivityLogEffectDispatcher activityLogEffectDispatcher = new(traceLogger);
        _activityLogStore = new MviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect>(
            ActivityLogState.Initial,
            activityLogIntentHandler,
            activityLogReducer,
            activityLogEffectDispatcher,
            Array.Empty<IMviMiddleware<ActivityLogState, ActivityLogIntent, ActivityLogEffect>>());

        // === 2. NavigationStore（叶子，无兄弟依赖） ===
        NavigationIntentHandler navigationIntentHandler = new();
        NavigationReducer navigationReducer = new();
        NavigationEffectDispatcher navigationEffectDispatcher = new(_activityLogStore, traceLogger);
        _navigationStore = new MviStore<NavigationState, NavigationIntent, NavigationEffect>(
            NavigationState.Initial,
            navigationIntentHandler,
            navigationReducer,
            navigationEffectDispatcher,
            Array.Empty<IMviMiddleware<NavigationState, NavigationIntent, NavigationEffect>>());

        // === 3. 创建 StoreReference 打破循环依赖 ===
        StoreReference<PlayerState, PlayerIntent, PlayerEffect> playerStoreRef = new();
        StoreReference<MissionState, MissionIntent, MissionEffect> missionStoreRef = new();
        StoreReference<HeroRosterState, HeroRosterIntent, HeroRosterEffect> heroRosterStoreRef = new();
        StoreReference<InventoryState, InventoryIntent, InventoryEffect> inventoryStoreRef = new();
        StoreReference<BattlePrepState, BattlePrepIntent, BattlePrepEffect> battlePrepStoreRef = new();

        // === 4. BattlePrepStore（IntentHandler 依赖循环中的 Store） ===
        BattlePrepIntentHandler battlePrepIntentHandler = new(
            apiService, playerStoreRef, missionStoreRef, heroRosterStoreRef, inventoryStoreRef);
        BattlePrepReducer battlePrepReducer = new();
        BattlePrepEffectDispatcher battlePrepEffectDispatcher = new(_activityLogStore, traceLogger);
        _battlePrepStore = new MviStore<BattlePrepState, BattlePrepIntent, BattlePrepEffect>(
            BattlePrepState.Initial,
            battlePrepIntentHandler,
            battlePrepReducer,
            battlePrepEffectDispatcher,
            Array.Empty<IMviMiddleware<BattlePrepState, BattlePrepIntent, BattlePrepEffect>>());
        battlePrepStoreRef.SetTarget(_battlePrepStore);

        // === 5. PlayerStore ===
        PlayerIntentHandler playerIntentHandler = new(
            apiService, missionStoreRef, heroRosterStoreRef, inventoryStoreRef);
        PlayerReducer playerReducer = new();
        PlayerEffectDispatcher playerEffectDispatcher = new(battlePrepStoreRef, _activityLogStore, traceLogger);
        _playerStore = new MviStore<PlayerState, PlayerIntent, PlayerEffect>(
            PlayerState.Initial,
            playerIntentHandler,
            playerReducer,
            playerEffectDispatcher,
            Array.Empty<IMviMiddleware<PlayerState, PlayerIntent, PlayerEffect>>());
        playerStoreRef.SetTarget(_playerStore);

        // === 6. MissionStore ===
        MissionIntentHandler missionIntentHandler = new(
            apiService, playerStoreRef, heroRosterStoreRef, inventoryStoreRef);
        MissionReducer missionReducer = new();
        MissionEffectDispatcher missionEffectDispatcher = new(
            playerStoreRef, battlePrepStoreRef, _activityLogStore, traceLogger);
        _missionStore = new MviStore<MissionState, MissionIntent, MissionEffect>(
            MissionState.Initial,
            missionIntentHandler,
            missionReducer,
            missionEffectDispatcher,
            Array.Empty<IMviMiddleware<MissionState, MissionIntent, MissionEffect>>());
        missionStoreRef.SetTarget(_missionStore);

        // === 7. HeroRosterStore ===
        HeroRosterIntentHandler heroRosterIntentHandler = new(
            apiService, playerStoreRef, missionStoreRef, inventoryStoreRef);
        HeroRosterReducer heroRosterReducer = new();
        HeroRosterEffectDispatcher heroRosterEffectDispatcher = new(
            playerStoreRef, battlePrepStoreRef, _activityLogStore, traceLogger);
        _heroRosterStore = new MviStore<HeroRosterState, HeroRosterIntent, HeroRosterEffect>(
            HeroRosterState.Initial,
            heroRosterIntentHandler,
            heroRosterReducer,
            heroRosterEffectDispatcher,
            Array.Empty<IMviMiddleware<HeroRosterState, HeroRosterIntent, HeroRosterEffect>>());
        heroRosterStoreRef.SetTarget(_heroRosterStore);

        // === 8. InventoryStore ===
        InventoryIntentHandler inventoryIntentHandler = new(
            apiService, playerStoreRef, missionStoreRef, heroRosterStoreRef);
        InventoryReducer inventoryReducer = new();
        InventoryEffectDispatcher inventoryEffectDispatcher = new(
            playerStoreRef, battlePrepStoreRef, _activityLogStore, traceLogger);
        _inventoryStore = new MviStore<InventoryState, InventoryIntent, InventoryEffect>(
            InventoryState.Initial,
            inventoryIntentHandler,
            inventoryReducer,
            inventoryEffectDispatcher,
            Array.Empty<IMviMiddleware<InventoryState, InventoryIntent, InventoryEffect>>());
        inventoryStoreRef.SetTarget(_inventoryStore);

        // === 9. ForgeLabStore（无状态 Store） ===
        ForgeLabIntentHandler forgeLabIntentHandler = new(
            apiService, playerStoreRef, missionStoreRef, heroRosterStoreRef, inventoryStoreRef);
        ForgeLabReducer forgeLabReducer = new();
        ForgeLabEffectDispatcher forgeLabEffectDispatcher = new(
            inventoryStoreRef, heroRosterStoreRef, battlePrepStoreRef, _activityLogStore, traceLogger);
        _forgeLabStore = new MviStore<UnitState, ForgeLabIntent, ForgeLabEffect>(
            UnitState.Instance,
            forgeLabIntentHandler,
            forgeLabReducer,
            forgeLabEffectDispatcher,
            Array.Empty<IMviMiddleware<UnitState, ForgeLabIntent, ForgeLabEffect>>());

        // === Navigator（依赖 PlayerStore 和 AppShellStore） ===
        GameShellNavigator navigator = new(_appShellStore, _playerStore);
        navigationEffectDispatcher.SetNavigator(navigator);

        // === Login Store ===
        LoginIntentHandler loginIntentHandler = new(new FakeAuthService());
        LoginReducer loginReducer = new();
        IReadOnlyList<IMviMiddleware<LoginState, LoginIntent, LoginEffect>> loginMiddlewares = [new LoginMiddleware()];
        LoginEffectDispatcher loginEffectDispatcher = new(navigator, traceLogger);
        _loginStore = new MviStore<LoginState, LoginIntent, LoginEffect>(
            new LoginState("miki", "123456", false, null, true, "演示账号已预填。点击登录会进入游戏大厅。密码长度至少 3 位。"),
            loginIntentHandler,
            loginReducer,
            loginEffectDispatcher,
            loginMiddlewares);

        // === ViewModels ===
        _loginViewModel = new LoginViewModel(_loginStore, uiDispatcher);

        // Lobby 8 个子 VM：3 个常驻 chrome + 5 个互斥面板，各自绑定独立 Store
        PlayerHeaderViewModel playerHeaderViewModel = new(_playerStore, _navigationStore, uiDispatcher);
        LobbyMenuViewModel lobbyMenuViewModel = new(_navigationStore, uiDispatcher);
        ActivityLogViewModel activityLogViewModel = new(_activityLogStore, uiDispatcher);
        MissionBoardViewModel missionBoardViewModel = new(_missionStore, _playerStore, uiDispatcher);
        HeroRosterViewModel heroRosterViewModel = new(_heroRosterStore, _playerStore, uiDispatcher);
        InventoryViewModel inventoryViewModel = new(_inventoryStore, _playerStore, uiDispatcher);
        ForgeLabViewModel forgeLabViewModel = new(_forgeLabStore, _inventoryStore, _heroRosterStore, uiDispatcher);
        BattlePrepViewModel battlePrepViewModel = new(_battlePrepStore, _missionStore, _heroRosterStore, _playerStore, uiDispatcher);

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
            _navigationStore,
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
    /// 由带 [MviSlot] 字段的 View 在源生成器 emit 的 OnBindSlots 钩子中解析。
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
        _forgeLabStore.Dispose();
        _inventoryStore.Dispose();
        _heroRosterStore.Dispose();
        _missionStore.Dispose();
        _playerStore.Dispose();
        _battlePrepStore.Dispose();
        _navigationStore.Dispose();
        _activityLogStore.Dispose();
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
    /// 表示 MVI Store 引用代理，用于打破 IntentHandler 之间的循环依赖。
    /// <para>
    /// 先创建引用占位，构造完真实 Store 后通过 <see cref="SetTarget"/> 注入；
    /// 所有成员委托给 target，target 未设置时调用即抛出。
    /// </para>
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TIntent">意图类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    private sealed class StoreReference<TState, TIntent, TEffect> : IMviStore<TState, TIntent, TEffect>
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        private IMviStore<TState, TIntent, TEffect>? _target;

        /// <summary>
        /// 设置真实 Store 目标。
        /// </summary>
        /// <param name="target">真实 Store 实例。</param>
        public void SetTarget(IMviStore<TState, TIntent, TEffect> target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>获取当前状态。</summary>
        public TState CurrentState
        {
            get
            {
                ArgumentNullException.ThrowIfNull(_target);
                return _target.CurrentState;
            }
        }

        /// <summary>获取状态变化流。</summary>
        public Observable<TState> States
        {
            get
            {
                ArgumentNullException.ThrowIfNull(_target);
                return _target.States;
            }
        }

        /// <summary>获取副作用变化流。</summary>
        public Observable<TEffect> Effects
        {
            get
            {
                ArgumentNullException.ThrowIfNull(_target);
                return _target.Effects;
            }
        }

        /// <summary>派发意图。</summary>
        /// <param name="intent">意图。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步派发过程的任务。</returns>
        public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_target);
            return _target.DispatchAsync(intent, cancellationToken);
        }

        /// <summary>释放资源（委托给 target）。</summary>
        public void Dispose()
        {
            _target?.Dispose();
        }
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
