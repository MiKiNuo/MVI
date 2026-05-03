using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 左侧菜单视图。
/// </summary>
public sealed partial class DashboardMenuView : MviAvaloniaView<DashboardMenuViewModel>
{
    /// <summary>
    /// 初始化 Dashboard 左侧菜单视图。
    /// </summary>
    public DashboardMenuView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
