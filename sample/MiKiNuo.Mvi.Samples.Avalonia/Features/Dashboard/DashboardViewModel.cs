﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳 ViewModel。
/// </summary>
public sealed partial class DashboardViewModel
    : MviViewModelBase<DashboardState, DashboardIntent, DashboardEffect>
{
    /// <summary>
    /// 初始化 Dashboard 壳 ViewModel。
    /// </summary>
    /// <param name="store">Dashboard 壳状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public DashboardViewModel(IMviStore<DashboardState, DashboardIntent, DashboardEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取当前登录显示名称。
    /// </summary>
    [MviBind(nameof(DashboardState.DisplayName))]
    public partial string DisplayName { get; private set; }

    /// <summary>
    /// 获取菜单 ViewModel。
    /// </summary>
    [MviBind(nameof(DashboardState.MenuViewModel))]
    public partial object MenuViewModel { get; private set; }

    /// <summary>
    /// 获取头部 ViewModel。
    /// </summary>
    [MviBind(nameof(DashboardState.HeaderViewModel))]
    public partial object HeaderViewModel { get; private set; }

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
    /// 获取当前页面 ViewModel。
    /// </summary>
    [MviBind(nameof(DashboardState.CurrentPageViewModel))]
    public partial object CurrentPageViewModel { get; private set; }
}
