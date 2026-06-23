﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳 ViewModel。
/// <para>
/// 子组件 ViewModel 全部脱离本 VM 的强类型属性：
/// </para>
/// <list type="bullet">
/// <item>菜单 / 头部：在 Shell 生命周期内静态不变，通过 <see cref="IDashboardChromeFactory"/>
///   工厂按 <see cref="CreateHeaderViewModel(string)"/> / <see cref="CreateMenuViewModel"/> 解析。</item>
/// <item>当前页面：随菜单切换而变化，State 仅保留 <see cref="CurrentPageKey"/> 判别器；
///   View 层通过 <see cref="IDashboardPageFactory"/>（由本 VM 经由 <see cref="CreateCurrentPageViewModel"/> 暴露）
///   按需创建页面 VM，避免父 VM 长期持有可变子 VM 引用。</item>
/// </list>
/// </summary>
public sealed partial class DashboardViewModel
    : MviViewModelBase<DashboardState, DashboardIntent, DashboardEffect>
{
    private readonly IDashboardChromeFactory _chromeFactory;
    private readonly IDashboardPageFactory _pageFactory;

    /// <summary>
    /// 初始化 Dashboard 壳 ViewModel。
    /// </summary>
    /// <param name="store">Dashboard 壳状态存储。</param>
    /// <param name="chromeFactory">菜单 / 头部子组件 ViewModel 工厂（Shell 生命周期内静态）。</param>
    /// <param name="pageFactory">把 PageKey 解析为具体页面 ViewModel 的工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public DashboardViewModel(
        IMviStore<DashboardState, DashboardIntent, DashboardEffect> store,
        IDashboardChromeFactory chromeFactory,
        IDashboardPageFactory pageFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(chromeFactory);
        ArgumentNullException.ThrowIfNull(pageFactory);

        _chromeFactory = chromeFactory;
        _pageFactory = pageFactory;
    }

    /// <summary>
    /// 获取当前登录显示名称。
    /// </summary>
    [MviBind(nameof(DashboardState.DisplayName))]
    public partial string DisplayName { get; private set; }

    /// <summary>
    /// 解析顶部头部子组件 ViewModel（经由 <see cref="IDashboardChromeFactory"/> 工厂按 displayName 缓存返回）。
    /// <para>
    /// 此无参重载供 <c>[MviSlot]</c> 源生成器 emit 调用，内部使用当前 <see cref="DisplayName"/> 作为参数。
    /// </para>
    /// </summary>
    /// <returns>头部 <c>HeaderViewModel</c> 实例。</returns>
    public object CreateHeaderViewModel() => _chromeFactory.CreateHeaderViewModel(DisplayName);

    /// <summary>
    /// 解析顶部头部子组件 ViewModel（经由 <see cref="IDashboardChromeFactory"/> 工厂按 displayName 缓存返回）。
    /// </summary>
    /// <param name="displayName">当前登录显示名称。</param>
    /// <returns>头部 <c>HeaderViewModel</c> 实例。</returns>
    public object CreateHeaderViewModel(string displayName) => _chromeFactory.CreateHeaderViewModel(displayName);

    /// <summary>
    /// 解析左侧菜单子组件 ViewModel（经由 <see cref="IDashboardChromeFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>菜单 <c>DashboardMenuViewModel</c> 实例。</returns>
    public object CreateMenuViewModel() => _chromeFactory.CreateMenuViewModel();

    /// <summary>
    /// 获取当前页面键（菜单驱动；View 层通过 <see cref="IDashboardPageFactory"/> 解析为具体页面 VM）。
    /// </summary>
    [MviBind(nameof(DashboardState.CurrentPageKey))]
    public partial string CurrentPageKey { get; private set; }

    /// <summary>
    /// 获取当前页面标题。
    /// </summary>
    [MviBind(nameof(DashboardState.CurrentPageTitle))]
    public partial string CurrentPageTitle { get; private set; }

    /// <summary>
    /// 获取当前页面说明。
    /// </summary>
    [MviBind(nameof(DashboardState.CurrentPageDescription))]
    public partial string CurrentPageDescription { get; private set; }

    /// <summary>
    /// 通过 <see cref="IDashboardPageFactory"/> 按 <see cref="CurrentPageKey"/> 解析当前页面 ViewModel。
    /// <para>
    /// 每次调用都重新走工厂；工厂内部可缓存页面 VM（如 <c>SampleGeneratedContainer</c> 那样在首次解析时构造并缓存）。
    /// 未识别的 PageKey 返回 null，由调用方决定兜底策略。
    /// </para>
    /// </summary>
    /// <returns>当前页面 ViewModel；未识别 PageKey 时返回 null。</returns>
    public object? CreateCurrentPageViewModel()
    {
        return _pageFactory.CreatePage(CurrentPageKey);
    }
}
