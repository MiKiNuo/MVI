using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板视图。
/// </summary>
public sealed partial class EventBindingDetailPanelView : MviAvaloniaView<EventBindingDetailViewModel>
{
    /// <summary>
    /// 初始化事件绑定详情面板视图。
    /// </summary>
    public EventBindingDetailPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
