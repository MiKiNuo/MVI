using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI视图。
/// </summary>
public sealed partial class AdmissionCoordinatorView : MviAvaloniaView<AdmissionCoordinatorViewModel>
{
    /// <summary>
    /// 初始化入院流程 MVI视图。
    /// </summary>
    public AdmissionCoordinatorView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
