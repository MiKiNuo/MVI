using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部视图。
/// </summary>
public sealed partial class HeaderView : MviAvaloniaView<HeaderViewModel>
{
    /// <summary>
    /// 初始化 Dashboard 头部视图。
    /// </summary>
    public HeaderView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
