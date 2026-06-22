using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板视图。
/// 通过 <c>ToEventSource().TextChanged</c> 把 <see cref="TextBox.TextChanged"/> 封装为
/// <see cref="IEventSource{TEvent}"/>，再用 <see cref="EventBinding{TEvent}"/> 映射为
/// <see cref="EventBindingSearchIntent.ChangeQuery"/> 意图，注册到 ViewModel 生命周期。
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

    /// <summary>
    /// 绑定 ViewModel 时注册事件绑定。
    /// </summary>
    /// <param name="viewModel">当前绑定的视图模型。</param>
    protected override void OnBind(EventBindingSearchViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        TextBox queryTextBox = this.FindControl<TextBox>("QueryTextBox")
            ?? throw new InvalidOperationException("未找到 QueryTextBox 控件。");

        string previousText = queryTextBox.Text ?? string.Empty;
        IEventSource<TextChangedEventArgs> source = queryTextBox.ToEventSource().TextChanged;
        EventBinding<TextChangedEventArgs> binding = new(
            source,
            args =>
            {
                string currentText = queryTextBox.Text ?? string.Empty;
                string prev = previousText;
                previousText = currentText;
                return new EventBindingSearchIntent.ChangeQuery(
                    new MviTextChangedEventPayload(currentText, prev, true, args));
            });
        viewModel.AddEventBinding(binding);
    }
}
