using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI视图。
/// </summary>
public sealed partial class LabOrderComposerView : MviAvaloniaView<LabOrderComposerViewModel>
{
    /// <summary>
    /// 初始化医嘱开立 MVI视图。
    /// </summary>
    public LabOrderComposerView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
