using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件视图。
/// </summary>
public sealed partial class StatisticsView : MviAvaloniaView<StatisticsViewModel>
{
    /// <summary>
    /// 初始化统计组件视图。
    /// </summary>
    public StatisticsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
