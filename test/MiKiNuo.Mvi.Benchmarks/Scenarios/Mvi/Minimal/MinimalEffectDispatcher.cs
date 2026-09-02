using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示最小基准场景副作用分发器：无操作执行并计数，供冒烟测试断言与基准对账。
/// </summary>
public sealed partial class MinimalEffectDispatcher
    : MviEffectDispatcherBase<MinimalIntent, MinimalEffect>
{
    /// <summary>
    /// 获取已执行的副作用总数。
    /// </summary>
    public int HandledCount { get; private set; }

    /// <summary>
    /// 执行第 1 号无操作副作用。
    /// </summary>
    [MviEffect(typeof(MinimalEffect.Nop1))]
    private ValueTask HandleNop1(MinimalEffect.Nop1 effect, CancellationToken cancellationToken)
    {
        HandledCount++;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 执行第 2 号无操作副作用。
    /// </summary>
    [MviEffect(typeof(MinimalEffect.Nop2))]
    private ValueTask HandleNop2(MinimalEffect.Nop2 effect, CancellationToken cancellationToken)
    {
        HandledCount++;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 执行第 3 号无操作副作用。
    /// </summary>
    [MviEffect(typeof(MinimalEffect.Nop3))]
    private ValueTask HandleNop3(MinimalEffect.Nop3 effect, CancellationToken cancellationToken)
    {
        HandledCount++;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 执行第 4 号无操作副作用。
    /// </summary>
    [MviEffect(typeof(MinimalEffect.Nop4))]
    private ValueTask HandleNop4(MinimalEffect.Nop4 effect, CancellationToken cancellationToken)
    {
        HandledCount++;
        return ValueTask.CompletedTask;
    }
}
