using global::Godot;
using MiKiNuo.Mvi.Application.MVI.EventBinding;

namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示 Godot 平台 <see cref="IEventSource{TEvent}"/> 适配器工厂。
/// 把 Godot 原生信号（<c>Button.Pressed</c>、<see cref="LineEdit.TextChanged"/> 等）
/// 封装为统一的 <see cref="IEventSource{TEvent}"/>，供 <see cref="EventBinding{TEvent}"/> 使用。
/// </summary>
/// <remarks>
/// 适配器只负责"订阅/取消订阅 Godot 信号"，不构造 payload——payload 构造由
/// <see cref="EventBinding{TEvent}"/> 的 Mapper 在 View 层完成，保持适配器轻量。
/// </remarks>
public static class GodotEventSources
{
    /// <summary>
    /// 把 <c>Button.Pressed</c> 信号封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="button">按钮控件。</param>
    /// <returns>无参数事件源（事件数据为 <see cref="EventArgs.Empty"/>）。</returns>
    public static IEventSource<EventArgs> FromPressed(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return new DelegateEventSource<EventArgs>(handler =>
        {
            Action adapter = () => handler(EventArgs.Empty);
            button.Pressed += adapter;
            return new ActionDisposable(() => button.Pressed -= adapter);
        });
    }

    /// <summary>
    /// 把 <see cref="LineEdit.TextChanged"/> 信号封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="lineEdit">文本输入控件。</param>
    /// <returns>文本变化事件源（事件数据为新文本字符串）。</returns>
    public static IEventSource<string> FromTextChanged(LineEdit lineEdit)
    {
        ArgumentNullException.ThrowIfNull(lineEdit);
        return new DelegateEventSource<string>(handler =>
        {
            LineEdit.TextChangedEventHandler adapter = text => handler(text);
            lineEdit.TextChanged += adapter;
            return new ActionDisposable(() => lineEdit.TextChanged -= adapter);
        });
    }

    /// <summary>
    /// 把 <see cref="ItemList.ItemSelected"/> 信号封装为 <see cref="IEventSource{TEvent}"/>。
    /// </summary>
    /// <param name="itemList">列表控件。</param>
    /// <returns>选中项事件源（事件数据为选中索引）。</returns>
    public static IEventSource<long> FromItemSelected(ItemList itemList)
    {
        ArgumentNullException.ThrowIfNull(itemList);
        return new DelegateEventSource<long>(handler =>
        {
            ItemList.ItemSelectedEventHandler adapter = index => handler(index);
            itemList.ItemSelected += adapter;
            return new ActionDisposable(() => itemList.ItemSelected -= adapter);
        });
    }
}
