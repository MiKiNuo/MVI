using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using MiKiNuo.Mvi.Application.MVI.EventBinding;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;

/// <summary>
/// 表示 Avalonia 平台 <see cref="IEventSource{TEvent}"/> 适配器工厂。
/// 把 Avalonia 原生事件（<see cref="Button.Click"/>、<see cref="TextBox.TextChanged"/> 等）
/// 封装为统一的 <see cref="IEventSource{TEvent}"/>，供 <see cref="EventBinding{TEvent}"/> 使用。
/// </summary>
/// <remarks>
/// 适配器只负责"订阅/取消订阅平台事件"，不构造 payload——payload 构造由
/// <see cref="EventBinding{TEvent}"/> 的 Mapper 在 View 层完成，保持适配器轻量。
/// </remarks>
public static class AvaloniaEventSources
{
    /// <summary>
    /// 把 <see cref="Button.Click"/> 事件封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="button">按钮控件。</param>
    /// <returns>路由事件源。</returns>
    public static IEventSource<RoutedEventArgs> FromClick(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return new DelegateEventSource<RoutedEventArgs>(handler =>
        {
            EventHandler<RoutedEventArgs> adapter = (_, e) => handler(e);
            button.Click += adapter;
            return new ActionDisposable(() => button.Click -= adapter);
        });
    }

    /// <summary>
    /// 把 <see cref="TextBox.TextChanged"/> 事件封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="textBox">文本框控件。</param>
    /// <returns>文本变化事件源。</returns>
    public static IEventSource<TextChangedEventArgs> FromTextChanged(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        return new DelegateEventSource<TextChangedEventArgs>(handler =>
        {
            EventHandler<TextChangedEventArgs> adapter = (_, e) => handler(e);
            textBox.TextChanged += adapter;
            return new ActionDisposable(() => textBox.TextChanged -= adapter);
        });
    }

    /// <summary>
    /// 把 <see cref="SelectingItemsControl.SelectionChanged"/> 事件封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="control">选择控件。</param>
    /// <returns>选择变化事件源。</returns>
    public static IEventSource<SelectionChangedEventArgs> FromSelectionChanged(SelectingItemsControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return new DelegateEventSource<SelectionChangedEventArgs>(handler =>
        {
            EventHandler<SelectionChangedEventArgs> adapter = (_, e) => handler(e);
            control.SelectionChanged += adapter;
            return new ActionDisposable(() => control.SelectionChanged -= adapter);
        });
    }

    /// <summary>
    /// 把 <see cref="InputElement.PointerPressed"/> 事件封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="element">输入元素。</param>
    /// <returns>指针按下事件源。</returns>
    public static IEventSource<PointerPressedEventArgs> FromPointerPressed(InputElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new DelegateEventSource<PointerPressedEventArgs>(handler =>
        {
            EventHandler<PointerPressedEventArgs> adapter = (_, e) => handler(e);
            element.PointerPressed += adapter;
            return new ActionDisposable(() => element.PointerPressed -= adapter);
        });
    }
}
