using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑视图。
/// </summary>
public sealed partial class ClinicalEditorView : MviAvaloniaView<ClinicalEditorViewModel>
{
    /// <summary>
    /// 初始化门诊病历编辑视图。
    /// </summary>
    public ClinicalEditorView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
