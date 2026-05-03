using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.CriticalValueMonitor;

/// <summary>
/// 表示危急值闭环 MVI视图。
/// </summary>
public sealed partial class CriticalValueMonitorView : MviAvaloniaView<CriticalValueMonitorViewModel>
{
    /// <summary>
    /// 初始化危急值闭环 MVI视图。
    /// </summary>
    public CriticalValueMonitorView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
