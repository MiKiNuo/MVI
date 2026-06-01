namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示基础指针事件载荷。
/// </summary>
/// <param name="PositionX">事件位置 X 坐标。</param>
/// <param name="PositionY">事件位置 Y 坐标。</param>
/// <param name="Button">指针按钮。</param>
/// <param name="ClickCount">点击次数。</param>
/// <param name="IsPressed">是否为按下事件。</param>
/// <param name="Modifiers">输入修饰键。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviPointerEventPayload(
    double PositionX,
    double PositionY,
    MviPointerButton Button,
    int ClickCount,
    bool IsPressed,
    MviInputModifiers Modifiers,
    object? RawEventArgs) : MviViewEventPayload(RawEventArgs);
