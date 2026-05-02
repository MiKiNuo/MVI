using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedOverview;

/// <summary>
/// 表示床位总览 MVI视图。
/// </summary>
public sealed partial class BedOverviewView : MviAvaloniaView<BedOverviewViewModel>
{
    /// <summary>
    /// 初始化床位总览 MVI视图。
    /// </summary>
    public BedOverviewView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
