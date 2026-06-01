using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板视图。
/// </summary>
public sealed partial class EventBindingSelectionPanelView : MviAvaloniaView<EventBindingSelectionViewModel>
{
    /// <summary>
    /// 初始化事件绑定选择面板视图。
    /// </summary>
    public EventBindingSelectionPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
