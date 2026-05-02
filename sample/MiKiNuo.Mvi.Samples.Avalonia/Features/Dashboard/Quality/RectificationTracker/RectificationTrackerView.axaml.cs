using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RectificationTracker;

/// <summary>
/// 表示整改闭环 MVI视图。
/// </summary>
public sealed partial class RectificationTrackerView : MviAvaloniaView<RectificationTrackerViewModel>
{
    /// <summary>
    /// 初始化整改闭环 MVI视图。
    /// </summary>
    public RectificationTrackerView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
