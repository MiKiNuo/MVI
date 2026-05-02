using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 视图。
/// </summary>
public sealed partial class PatientSearchView : MviAvaloniaView<PatientSearchViewModel>
{
    /// <summary>
    /// 初始化可复用患者检索 MVI 视图。
    /// </summary>
    public PatientSearchView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
