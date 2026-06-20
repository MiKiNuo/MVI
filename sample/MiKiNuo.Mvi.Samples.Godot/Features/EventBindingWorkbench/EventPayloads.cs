namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 平台显式动作事件载荷。
/// </summary>
/// <param name="SourceName">事件来源名称。</param>
/// <param name="ActionName">动作名称。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviActionEventPayload(
    string? SourceName,
    string? ActionName,
    object? RawEventArgs);

/// <summary>
/// 表示 Godot 平台文本变化事件载荷。
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
/// 表示 Godot 平台选择变化事件载荷。
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
