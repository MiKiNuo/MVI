using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示最小基准场景意图。
/// </summary>
public abstract partial record MinimalIntent : IMviIntent
{
    /// <summary>
    /// 表示计数递增意图：无副作用路径，测量纯派发管线。
    /// </summary>
    public sealed partial record Increment : MinimalIntent;

    /// <summary>
    /// 表示按规约器配置数量产出无操作副作用的意图，测量副作用派发路径。
    /// </summary>
    public sealed partial record EmitNops : MinimalIntent;
}
