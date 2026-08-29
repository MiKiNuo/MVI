using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Reducer;

/// <summary>
/// 表示 MVI 规约器基类。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public abstract class MviReducerBase<TState, TIntent, TEffect>
    : IMviReducer<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public abstract MviReduceResult<TState, TEffect> Reduce(
        TState state,
        TIntent intent);

    /// <summary>
    /// 返回状态不变的规约结果（无副作用）。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <returns>仅包含状态的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> Unchanged(TState state)
    {
        return MviReduceResult.State<TState, TEffect>(state);
    }

    /// <summary>
    /// 返回新状态与单个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effect">副作用。</param>
    /// <returns>包含状态与副作用的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffect(
        TState state,
        TEffect effect)
    {
        return MviReduceResult.StateAndEffect<TState, TEffect>(state, effect);
    }

    /// <summary>
    /// 返回新状态与多个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effects">副作用集合。</param>
    /// <returns>包含状态与副作用集合的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffects(
        TState state,
        IReadOnlyList<TEffect> effects)
    {
        return MviReduceResult.StateAndEffects<TState, TEffect>(state, effects);
    }

    /// <summary>
    /// 返回新状态与多个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effects">副作用数组。</param>
    /// <returns>包含状态与副作用集合的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffects(
        TState state,
        params TEffect[] effects)
    {
        return MviReduceResult.StateAndEffects<TState, TEffect>(state, effects);
    }
}
