using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.WardRiskPanel;

/// <summary>
/// 表示病区风险 MVI视图。
/// </summary>
public sealed partial class WardRiskPanelView : MviAvaloniaView<WardRiskPanelViewModel>
{
    /// <summary>
    /// 初始化病区风险 MVI视图。
    /// </summary>
    public WardRiskPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
