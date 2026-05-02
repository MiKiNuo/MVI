using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 视图。
/// </summary>
public sealed partial class AuditTimelineView : MviAvaloniaView<AuditTimelineViewModel>
{
    /// <summary>
    /// 初始化可复用审计时间线 MVI 视图。
    /// </summary>
    public AuditTimelineView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
