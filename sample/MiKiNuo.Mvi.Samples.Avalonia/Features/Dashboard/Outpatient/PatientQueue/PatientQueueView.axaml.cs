using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列视图。
/// </summary>
public sealed partial class PatientQueueView : MviAvaloniaView<PatientQueueViewModel>
{
    /// <summary>
    /// 初始化门诊队列视图。
    /// </summary>
    public PatientQueueView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
