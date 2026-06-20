using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板视图。
/// 通过 <see cref="AvaloniaEventSources.FromPointerPressed"/> 和 <see cref="AvaloniaEventSources.FromClick"/>
/// 把 <see cref="InputElement.PointerPressed"/> 与 <see cref="Button.Click"/> 封装为
/// <see cref="IEventSource{TEvent}"/>，再用 <see cref="EventBinding{TEvent}"/> 映射为
/// <see cref="EventBindingDetailIntent.PressDetail"/> / <see cref="EventBindingDetailIntent.Refresh"/> 意图。
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

    /// <inheritdoc />
    protected override void OnBind(EventBindingDetailViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        Control pointerSurface = this.FindControl<Control>("PointerSurface")
            ?? throw new InvalidOperationException("未找到 PointerSurface 控件。");
        Button refreshButton = this.FindControl<Button>("DetailRefreshButton")
            ?? throw new InvalidOperationException("未找到 DetailRefreshButton 控件。");

        // 指针按下 → PressDetail 意图
        IEventSource<PointerPressedEventArgs> pointerSource =
            AvaloniaEventSources.FromPointerPressed(pointerSurface);
        EventBinding<PointerPressedEventArgs> pointerBinding = new(
            pointerSource,
            args => new EventBindingDetailIntent.PressDetail(
                AvaloniaEventPayloads.ToPointerPayload(pointerSurface, args)));
        viewModel.AddEventBinding(pointerBinding);

        // 刷新按钮点击 → Refresh 意图
        IEventSource<RoutedEventArgs> clickSource =
            AvaloniaEventSources.FromClick(refreshButton);
        EventBinding<RoutedEventArgs> clickBinding = new(
            clickSource,
            args => new EventBindingDetailIntent.Refresh(
                AvaloniaEventPayloads.ToActionPayload(refreshButton, args, "Refresh")));
        viewModel.AddEventBinding(clickBinding);
    }
}
