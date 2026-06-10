﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳 ViewModel。
/// <para>
/// 3 个子组件 ViewModel 全部脱离 State：
/// </para>
/// <list type="bullet">
/// <item>菜单 / 头部：在 Shell 生命周期内静态不变，构造函数注入并暴露为只读属性。</item>
/// <item>当前页面：随菜单切换而变化，State 仅保留 <see cref="CurrentPageKey"/> 判别器；
///   View 层通过 <see cref="IDashboardPageFactory"/> 按需创建页面 VM，避免父 VM 长期持有可变子 VM 引用。</item>
/// </list>
/// </summary>
public sealed partial class DashboardViewModel
    : MviViewModelBase<DashboardState, DashboardIntent, DashboardEffect>
{
    private readonly IDashboardPageFactory _pageFactory;

    /// <summary>
    /// 初始化 Dashboard 壳 ViewModel。
    /// </summary>
    /// <param name="store">Dashboard 壳状态存储。</param>
    /// <param name="menuViewModel">左侧菜单子组件 ViewModel（Shell 生命周期内静态）。</param>
    /// <param name="headerViewModel">顶部头部子组件 ViewModel（Shell 生命周期内静态）。</param>
    /// <param name="pageFactory">把 PageKey 解析为具体页面 ViewModel 的工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public DashboardViewModel(
        IMviStore<DashboardState, DashboardIntent, DashboardEffect> store,
        DashboardMenuViewModel menuViewModel,
        HeaderViewModel headerViewModel,
        IDashboardPageFactory pageFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(menuViewModel);
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(pageFactory);

        MenuViewModel = menuViewModel;
        HeaderViewModel = headerViewModel;
        _pageFactory = pageFactory;
    }

    /// <summary>
    /// 获取当前登录显示名称。
    /// </summary>
    [MviBind(nameof(DashboardState.DisplayName))]
    public partial string DisplayName { get; private set; }

    /// <summary>
    /// 获取左侧菜单 ViewModel（构造函数注入，Shell 生命周期内静态）。
    /// </summary>
    public DashboardMenuViewModel MenuViewModel { get; }

    /// <summary>
    /// 获取顶部头部 ViewModel（构造函数注入，Shell 生命周期内静态）。
    /// </summary>
    public HeaderViewModel HeaderViewModel { get; }

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
