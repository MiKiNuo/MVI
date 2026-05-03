using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒视图。
/// </summary>
public sealed partial class ClinicalReminderView : MviAvaloniaView<ClinicalReminderViewModel>
{
    /// <summary>
    /// 初始化临床提醒视图。
    /// </summary>
    public ClinicalReminderView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
