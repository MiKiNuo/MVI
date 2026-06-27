namespace MiKiNuo.Mvi.Samples.Shared.Tracing;

/// <summary>
/// 表示追踪副作用的跨平台契约。
/// </summary>
/// <remarks>
/// 各 Feature 的 Trace Effect 实现此接口，使日志记录器可统一处理。
/// </remarks>
public interface ITraceEffect
{
    /// <summary>
    /// 获取追踪文本。
    /// </summary>
    public string Text { get; }
}
