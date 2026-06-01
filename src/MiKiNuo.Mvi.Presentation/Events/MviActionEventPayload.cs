namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示显式动作事件载荷。
/// </summary>
/// <param name="SourceName">事件来源名称。</param>
/// <param name="ActionName">动作名称。</param>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public sealed record MviActionEventPayload(
    string? SourceName,
    string? ActionName,
    object? RawEventArgs) : MviViewEventPayload(RawEventArgs);
