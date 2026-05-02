using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeaturesCards;

/// <summary>
/// 表示业务指标卡片视图。
/// </summary>
public sealed partial class MetricCardView : MviAvaloniaView<MetricCardViewModel>
{
    /// <summary>
    /// 初始化业务指标卡片视图。
    /// </summary>
    public MetricCardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
