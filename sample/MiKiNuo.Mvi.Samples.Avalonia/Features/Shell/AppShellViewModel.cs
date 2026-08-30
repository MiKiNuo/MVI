using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳 ViewModel。
/// </summary>
public sealed partial class AppShellViewModel
    : MviViewModelBase<AppShellState, AppShellIntent, UnitEffect>
{
    /// <summary>
    /// 初始化应用壳 ViewModel。
    /// </summary>
    /// <param name="store">应用壳状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public AppShellViewModel(
        IMviStore<AppShellState, AppShellIntent, UnitEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取当前页面。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentPage))]
    public partial ShellPage CurrentPage { get; private set; }

    /// <summary>
    /// 获取已登录用户显示名。
    /// </summary>
    [MviBind(nameof(AppShellState.DisplayName))]
    public partial string? DisplayName { get; private set; }
}
