namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示跨平台 View 事件载荷基类。
/// </summary>
/// <param name="RawEventArgs">平台原生事件参数。</param>
public abstract record MviViewEventPayload(object? RawEventArgs);
