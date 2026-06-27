using global::Godot;

namespace MiKiNuo.Mvi.Samples.Godot.Tracing;

/// <summary>
/// 表示 Godot 平台的追踪日志记录器。
/// </summary>
public sealed class GodotTraceEffectLogger : ITraceEffectLogger
{
    /// <summary>
    /// 记录追踪副作用到 Godot 控制台。
    /// </summary>
    /// <param name="effect">追踪副作用。</param>
    public void Log(ITraceEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        GD.Print($"[Godot MVI Trace] {effect.Text}");
    }
}
