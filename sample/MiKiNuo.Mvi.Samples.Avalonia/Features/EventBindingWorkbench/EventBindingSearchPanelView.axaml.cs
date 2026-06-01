using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板视图。
/// </summary>
public sealed partial class EventBindingSearchPanelView : MviAvaloniaView<EventBindingSearchViewModel>
{
    /// <summary>
    /// 初始化事件绑定搜索面板视图。
    /// </summary>
    public EventBindingSearchPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
