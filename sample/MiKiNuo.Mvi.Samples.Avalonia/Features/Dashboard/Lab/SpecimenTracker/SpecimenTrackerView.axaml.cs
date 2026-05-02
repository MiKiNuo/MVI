using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.SpecimenTracker;

/// <summary>
/// 表示标本流转 MVI视图。
/// </summary>
public sealed partial class SpecimenTrackerView : MviAvaloniaView<SpecimenTrackerViewModel>
{
    /// <summary>
    /// 初始化标本流转 MVI视图。
    /// </summary>
    public SpecimenTrackerView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
