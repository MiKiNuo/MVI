using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 左侧菜单 ViewModel。
/// </summary>
public sealed partial class DashboardMenuViewModel
    : MviViewModelBase<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect>
{
    /// <summary>
    /// 初始化 Dashboard 左侧菜单 ViewModel。
    /// </summary>
    /// <param name="store">菜单状态存储。</param>
    public DashboardMenuViewModel(IMviStore<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取菜单项集合。
    /// </summary>
    [MviBind(nameof(DashboardMenuState.MenuItems))]
    public partial IReadOnlyList<string> MenuItems { get; private set; }

    /// <summary>
    /// 获取或设置当前选中的菜单键。
    /// </summary>
    [MviBind(
        nameof(DashboardMenuState.SelectedMenuKey),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(DashboardMenuIntent.SelectMenuKey))]
    public partial string SelectedMenuKey { get; set; }

    /// <summary>
    /// 获取菜单状态文本。
    /// </summary>
    [MviBind(nameof(DashboardMenuState.StatusText))]
    public partial string StatusText { get; private set; }
}
