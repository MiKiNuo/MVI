using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.NursingTaskBoard;

/// <summary>
/// 表示护理任务 MVI视图。
/// </summary>
public sealed partial class NursingTaskBoardView : MviAvaloniaView<NursingTaskBoardViewModel>
{
    /// <summary>
    /// 初始化护理任务 MVI视图。
    /// </summary>
    public NursingTaskBoardView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
