using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.QualityKpiBoard;

/// <summary>
/// 表示质量 KPI MVI视图。
/// </summary>
public sealed partial class QualityKpiBoardView : MviAvaloniaView<QualityKpiBoardViewModel>
{
    /// <summary>
    /// 初始化质量 KPI MVI视图。
    /// </summary>
    public QualityKpiBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
