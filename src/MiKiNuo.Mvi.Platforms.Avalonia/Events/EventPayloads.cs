namespace MiKiNuo.Mvi.Platforms.Avalonia.Events;

/// <summary>
/// 表示 Avalonia 平台指针按钮。
/// </summary>
public enum MviPointerButton
{
    /// <summary>未知或无按钮。</summary>
    None,

    /// <summary>左键或主按钮。</summary>
    Left,

    /// <summary>右键或次按钮。</summary>
    Right,

    /// <summary>中键。</summary>
    Middle,

    /// <summary>第一扩展按钮。</summary>
    XButton1,

    /// <summary>第二扩展按钮。</summary>
    XButton2,

    /// <summary>触摸输入。</summary>
    Touch,

    /// <summary>笔输入。</summary>
    Pen
}

/// <summary>
/// 表示 Avalonia 平台输入修饰键。
/// </summary>
[Flags]
public enum MviInputModifiers
{
    /// <summary>无修饰键。</summary>
    None = 0,

    /// <summary>Shift 修饰键。</summary>
    Shift = 1,

    /// <summary>Control 修饰键。</summary>
    Control = 2,

    /// <summary>Alt 修饰键。</summary>
    Alt = 4,

    /// <summary>Meta 或 Command 修饰键。</summary>
    Meta = 8
}

/// <summary>
/// 表示显式动作事件载荷。
/// </summary>
/// <param name="SourceName">事件来源名称。</param>
/// <param name="ActionName">动作名称。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviActionEventPayload(
    string? SourceName,
    string? ActionName,
    object? RawEventArgs);

/// <summary>
/// 表示文本变化事件载荷。
/// </summary>
/// <param name="Text">当前文本。</param>
/// <param name="PreviousText">上一次文本。</param>
/// <param name="IsUserInitiated">是否由用户输入触发。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviTextChangedEventPayload(
    string Text,
    string? PreviousText,
    bool IsUserInitiated,
    object? RawEventArgs);

/// <summary>
/// 表示选择变化事件载荷。
/// </summary>
/// <param name="SelectedValue">当前选中值。</param>
/// <param name="SelectedIndex">当前选中索引。</param>
/// <param name="PreviousSelectedValue">上一次选中值。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviSelectionChangedEventPayload(
    object? SelectedValue,
    int? SelectedIndex,
    object? PreviousSelectedValue,
    object? RawEventArgs);

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
    object? RawEventArgs);
