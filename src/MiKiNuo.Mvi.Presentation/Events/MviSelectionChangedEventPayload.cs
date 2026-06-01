namespace MiKiNuo.Mvi.Presentation.Events;

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
    object? RawEventArgs) : MviViewEventPayload(RawEventArgs);
