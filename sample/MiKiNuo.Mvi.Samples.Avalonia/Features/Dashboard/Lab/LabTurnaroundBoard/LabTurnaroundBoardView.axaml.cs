using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabTurnaroundBoard;

/// <summary>
/// 表示TAT 监控 MVI视图。
/// </summary>
public sealed partial class LabTurnaroundBoardView : MviAvaloniaView<LabTurnaroundBoardViewModel>
{
    /// <summary>
    /// 初始化TAT 监控 MVI视图。
    /// </summary>
    public LabTurnaroundBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
