using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅组合根 ViewModel。
/// <para>
/// 所有 8 个子组件 ViewModel（3 个常驻 chrome + 5 个互斥面板）全部脱离本 VM 的强类型属性：
/// </para>
/// <list type="bullet">
/// <item>常驻 chrome（玩家头部 / 大厅菜单 / 活动日志）：通过 <see cref="ILobbyChromeFactory"/>
///   工厂按 <see cref="CreateHeaderViewModel"/> / <see cref="CreateMenuViewModel"/> /
///   <see cref="CreateActivityLogViewModel"/> 解析。</item>
/// <item>互斥面板（任务大厅 / 英雄队伍 / 背包仓库 / 锻造工坊 / 战斗准备）：通过
///   <see cref="ILobbyPanelFactory"/> 工厂按 <see cref="CreateCurrentPanelViewModel"/> + <see cref="CurrentPanel"/> 解析。</item>
/// </list>
/// <para>
/// 绑定 <c>IMviStore&lt;NavigationState, NavigationIntent, NavigationEffect&gt;</c>：本 VM 仅承载面板切换判别器，子 VM 通过各自 Store 驱动。</para>
/// </summary>
public sealed partial class LobbyViewModel : MviViewModelBase<NavigationState, NavigationIntent, NavigationEffect>
{
    private readonly ILobbyChromeFactory _chromeFactory;
    private readonly ILobbyPanelFactory _panelFactory;

    /// <summary>
    /// 初始化游戏大厅组合根 ViewModel。
    /// </summary>
    /// <param name="store">大厅导航状态存储。</param>
    /// <param name="chromeFactory">3 个常驻 chrome 子组件 ViewModel 工厂。</param>
    /// <param name="panelFactory">5 个互斥面板 ViewModel 工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public LobbyViewModel(
        IMviStore<NavigationState, NavigationIntent, NavigationEffect> store,
        ILobbyChromeFactory chromeFactory,
        ILobbyPanelFactory panelFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(chromeFactory);
        ArgumentNullException.ThrowIfNull(panelFactory);

        _chromeFactory = chromeFactory;
        _panelFactory = panelFactory;
    }

    /// <summary>获取当前大厅面板。</summary>
    [MviBind(nameof(NavigationState.CurrentPanel))]
    public partial LobbyPanel CurrentPanel { get; private set; }

    /// <summary>
    /// 解析玩家头部子组件 ViewModel（经由 <see cref="ILobbyChromeFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>玩家头部 <c>PlayerHeaderViewModel</c> 实例。</returns>
    public object CreateHeaderViewModel() => _chromeFactory.CreateHeaderViewModel();

    /// <summary>
    /// 解析大厅菜单子组件 ViewModel（经由 <see cref="ILobbyChromeFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>大厅菜单 <c>LobbyMenuViewModel</c> 实例。</returns>
    public object CreateMenuViewModel() => _chromeFactory.CreateMenuViewModel();

    /// <summary>
    /// 解析活动日志子组件 ViewModel（经由 <see cref="ILobbyChromeFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>活动日志 <c>ActivityLogViewModel</c> 实例。</returns>
    public object CreateActivityLogViewModel() => _chromeFactory.CreateActivityLogViewModel();

    /// <summary>
    /// 按 <see cref="CurrentPanel"/> 通过 <see cref="ILobbyPanelFactory"/> 解析当前可见面板的 ViewModel。
    /// <para>
    /// 5 个互斥面板由各自独立 Store 驱动，由工厂内部缓存避免每次切换面板都重新构造；
    /// 未识别 <see cref="CurrentPanel"/> 时返回 null。
    /// </para>
    /// </summary>
    /// <returns>当前可见面板的 ViewModel；未识别 <see cref="CurrentPanel"/> 时返回 null。</returns>
    public object? CreateCurrentPanelViewModel()
    {
        return _panelFactory.CreatePanel(CurrentPanel);
    }
}
