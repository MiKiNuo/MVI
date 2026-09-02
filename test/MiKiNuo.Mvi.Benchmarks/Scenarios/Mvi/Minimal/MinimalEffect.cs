using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示最小基准场景副作用：四个无操作子类型，供副作用数量扫描（0/1/4）使用。
/// </summary>
public abstract partial record MinimalEffect : IMviEffect
{
    /// <summary>
    /// 表示第 1 号无操作副作用。
    /// </summary>
    public sealed partial record Nop1 : MinimalEffect;

    /// <summary>
    /// 表示第 2 号无操作副作用。
    /// </summary>
    public sealed partial record Nop2 : MinimalEffect;

    /// <summary>
    /// 表示第 3 号无操作副作用。
    /// </summary>
    public sealed partial record Nop3 : MinimalEffect;

    /// <summary>
    /// 表示第 4 号无操作副作用。
    /// </summary>
    public sealed partial record Nop4 : MinimalEffect;
}
