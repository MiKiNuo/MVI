namespace MiKiNuo.Mvi.Samples.Shared.Tracing;

/// <summary>
/// 表示追踪日志记录器契约。
/// </summary>
/// <remarks>
/// 各平台 sample 实现此接口以提供平台特定的日志输出（如 Godot 的 GD.Print）。
/// </remarks>
public interface ITraceEffectLogger
{
    /// <summary>
    /// 记录追踪副作用。
    /// </summary>
    /// <param name="effect">追踪副作用。</param>
    public void Log(ITraceEffect effect);
}
