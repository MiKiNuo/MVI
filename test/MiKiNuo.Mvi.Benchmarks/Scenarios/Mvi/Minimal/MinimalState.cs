using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示最小基准场景状态：仅含一个计数器，用于剥离业务噪音、测量框架底噪。
/// </summary>
/// <param name="Counter">已完成的意图计数。</param>
public sealed record MinimalState(int Counter) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static MinimalState Initial { get; } = new(0);
}
