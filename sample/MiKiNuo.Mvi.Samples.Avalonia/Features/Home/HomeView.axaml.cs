using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页视图。
/// </summary>
public sealed partial class HomeView : MviAvaloniaView<HomeViewModel>
{
    /// <summary>
    /// 初始化主页视图。
    /// </summary>
    public HomeView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
