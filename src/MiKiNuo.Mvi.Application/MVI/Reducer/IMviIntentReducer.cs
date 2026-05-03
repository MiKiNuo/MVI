using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Reducer;

/// <summary>
/// 表示面向单个意图类型的 MVI 规约处理器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TFeatureIntent">具体意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviIntentReducer<TState, TFeatureIntent, TEffect>
    where TState : IMviState
    where TFeatureIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 根据当前状态和具体意图生成规约结果。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">具体意图。</param>
    /// <returns>规约结果。</returns>
    public MviReduceResult<TState, TEffect> Reduce(TState state, TFeatureIntent intent);
}
