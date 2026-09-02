using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示最小基准场景规约器：EmitNops 意图按构造参数产出 0/1/4 个无操作副作用。
/// </summary>
public sealed partial class MinimalReducer
    : MviReducerBase<MinimalState, MinimalIntent, MinimalEffect>
{
    private readonly int _effectCount;

    /// <summary>
    /// 初始化最小基准场景规约器。
    /// </summary>
    /// <param name="effectCount">EmitNops 意图产出的无操作副作用数量，取值 0、1 或 4。</param>
    public MinimalReducer(int effectCount)
    {
        _effectCount = effectCount;
    }

    /// <summary>
    /// 处理计数递增意图：产出计数加一的新状态，无副作用。
    /// </summary>
    [MviReduce(typeof(MinimalIntent.Increment))]
    private MviReduceResult<MinimalState, MinimalEffect> HandleIncrement(
        MinimalState state,
        MinimalIntent.Increment intent)
    {
        return Unchanged(state with { Counter = state.Counter + 1 });
    }

    /// <summary>
    /// 处理产出副作用的意图：计数加一并按配置数量产出无操作副作用。
    /// </summary>
    [MviReduce(typeof(MinimalIntent.EmitNops))]
    private MviReduceResult<MinimalState, MinimalEffect> HandleEmitNops(
        MinimalState state,
        MinimalIntent.EmitNops intent)
    {
        return WithEffects(state with { Counter = state.Counter + 1 }, BuildNops());
    }

    /// <summary>
    /// 按配置数量构造无操作副作用数组。
    /// </summary>
    /// <returns>无操作副作用数组。</returns>
    private MinimalEffect[] BuildNops()
    {
        return _effectCount switch
        {
            1 => new MinimalEffect[] { new MinimalEffect.Nop1() },
            4 => new MinimalEffect[]
            {
                new MinimalEffect.Nop1(),
                new MinimalEffect.Nop2(),
                new MinimalEffect.Nop3(),
                new MinimalEffect.Nop4(),
            },
            _ => Array.Empty<MinimalEffect>(),
        };
    }
}
