using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.MedicationSafetyPanel;

/// <summary>
/// 表示用药安全 MVI视图。
/// </summary>
public sealed partial class MedicationSafetyPanelView : MviAvaloniaView<MedicationSafetyPanelViewModel>
{
    /// <summary>
    /// 初始化用药安全 MVI视图。
    /// </summary>
    public MedicationSafetyPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
