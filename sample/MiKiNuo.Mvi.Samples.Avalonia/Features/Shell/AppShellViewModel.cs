using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳 ViewModel。
/// </summary>
public sealed partial class AppShellViewModel
    : MviViewModelBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 初始化应用壳 ViewModel。
    /// </summary>
    /// <param name="store">应用壳状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public AppShellViewModel(IMviStore<AppShellState, AppShellIntent, AppShellEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取窗口标题。
    /// </summary>
    [MviBind(nameof(AppShellState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取当前页面 ViewModel。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentViewModel))]
    public partial object? CurrentViewModel { get; private set; }

    /// <summary>
    /// 显示页面。
    /// </summary>
    /// <param name="title">标题。</param>
    /// <param name="viewModel">页面 ViewModel。</param>
    /// <returns>表示异步切换过程的任务。</returns>
    public ValueTask ShowPageAsync(string title, object viewModel)
    {
        return DispatchAsync(new AppShellIntent.ShowPage(title, viewModel));
    }
}
