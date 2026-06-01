namespace MiKiNuo.Mvi.Presentation.Events;

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
    object? RawEventArgs) : MviViewEventPayload(RawEventArgs);
