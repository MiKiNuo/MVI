using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳 ViewModel。
/// <para>
/// 不再持有任何页面 <c>*ViewModel</c> 引用；
/// 顶层页面 VM 通过 <see cref="IShellPageFactory"/> 按 <see cref="CurrentPageKey"/> 解析。
/// </para>
/// </summary>
public sealed partial class AppShellViewModel
    : MviViewModelBase<AppShellState, AppShellIntent, UnitEffect>
{
    private readonly IShellPageFactory _pageFactory;

    /// <summary>
    /// 初始化应用壳 ViewModel。
    /// </summary>
    /// <param name="store">应用壳状态存储。</param>
    /// <param name="pageFactory">顶层页面 ViewModel 工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public AppShellViewModel(
        IMviStore<AppShellState, AppShellIntent, UnitEffect> store,
        IShellPageFactory pageFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(pageFactory);
        _pageFactory = pageFactory;
    }

    /// <summary>
    /// 获取当前顶层页面键。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentPageKey))]
    public partial string CurrentPageKey { get; private set; }

    /// <summary>
    /// 获取窗口标题。
    /// </summary>
    [MviBind(nameof(AppShellState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 按 <see cref="CurrentPageKey"/> 通过 <see cref="IShellPageFactory"/> 解析当前页面 ViewModel。
    /// </summary>
    /// <returns>页面 ViewModel；未识别 PageKey 时返回 null。</returns>
    public object? CreateCurrentPageViewModel()
    {
        return _pageFactory.CreatePage(CurrentPageKey);
    }

    /// <summary>
    /// 显示页面。
    /// </summary>
    /// <param name="pageKey">页面键。</param>
    /// <param name="title">标题。</param>
    /// <returns>表示异步切换过程的任务。</returns>
    public ValueTask ShowPageAsync(string pageKey, string title)
    {
        return DispatchAsync(AppShellIntent.CreateShowPage(pageKey, title));
    }
}
