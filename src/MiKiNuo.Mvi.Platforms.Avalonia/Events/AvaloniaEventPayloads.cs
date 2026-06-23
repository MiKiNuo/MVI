using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;

/// <summary>
/// 表示 Avalonia 平台事件参数到 <see cref="MviActionEventPayload"/> / <see cref="MviPointerEventPayload"/> 等载荷的映射帮助方法。
/// View 层在构造 <see cref="Application.MVI.EventBinding.EventBinding{TEvent}"/> 的 Mapper 时复用本类方法，
/// 避免 payload 构造逻辑散落在各 View 代码中。
/// </summary>
public static class AvaloniaEventPayloads
{
    /// <summary>
    /// 把 <see cref="RoutedEventArgs"/> 映射为 <see cref="MviActionEventPayload"/>。
    /// </summary>
    /// <param name="source">事件来源控件（用于取 <see cref="StyledElement.Name"/>）。</param>
    /// <param name="args">路由事件参数。</param>
    /// <param name="actionName">动作名称。</param>
    /// <returns>动作事件载荷。</returns>
    public static MviActionEventPayload ToActionPayload(
        AvaloniaObject source,
        RoutedEventArgs args,
        string? actionName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(args);
        string? sourceName = source is Control control ? control.Name : null;
        return new MviActionEventPayload(sourceName, actionName, args);
    }

    /// <summary>
    /// 把 <see cref="PointerPressedEventArgs"/> 映射为 <see cref="MviPointerEventPayload"/>。
    /// </summary>
    /// <param name="element">输入元素（用于取事件相对坐标）。</param>
    /// <param name="args">指针按下事件参数。</param>
    /// <returns>指针事件载荷。</returns>
    public static MviPointerEventPayload ToPointerPayload(InputElement element, PointerPressedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(args);

        Point position = args.GetPosition(element);
        PointerPoint point = args.GetCurrentPoint(element);
        return new MviPointerEventPayload(
            position.X,
            position.Y,
            MapPointerButton(point.Properties.PointerUpdateKind),
            args.ClickCount,
            true,
            MapModifiers(args.KeyModifiers),
            args);
    }

    private static MviPointerButton MapPointerButton(PointerUpdateKind pointerUpdateKind)
    {
        return pointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MviPointerButton.Left,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MviPointerButton.Right,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MviPointerButton.Middle,
            PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => MviPointerButton.XButton1,
            PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => MviPointerButton.XButton2,
            _ => MviPointerButton.None,
        };
    }

    private static MviInputModifiers MapModifiers(KeyModifiers modifiers)
    {
        MviInputModifiers result = MviInputModifiers.None;

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= MviInputModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= MviInputModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= MviInputModifiers.Alt;
        }

        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= MviInputModifiers.Meta;
        }

        return result;
    }
}
