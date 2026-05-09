using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Domain.MVI.Reducer;

/// <summary>
/// 表示 MVI 规约结果。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="State">规约后的状态。</param>
/// <param name="Effects">规约过程中产生的副作用集合。</param>
public sealed record MviReduceResult<TState, TEffect>(
    TState State,
    IReadOnlyList<TEffect> Effects)
    where TState : IMviState
    where TEffect : IMviEffect;

/// <summary>
/// 表示 MVI 规约结果工厂。
/// </summary>
public static class MviReduceResult
{
    /// <summary>
    /// 创建仅包含状态的规约结果。
    /// 使用 Array.Empty 共享引用避免每次分配新数组。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">规约后的状态。</param>
    /// <returns>规约结果。</returns>
    public static MviReduceResult<TState, TEffect> State<TState, TEffect>(TState state)
        where TState : IMviState
        where TEffect : IMviEffect
    {
        return new MviReduceResult<TState, TEffect>(state, System.Array.Empty<TEffect>());
    }

    /// <summary>
    /// 创建同时包含状态和一个副作用的规约结果。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effect">副作用。</param>
    /// <returns>规约结果。</returns>
    public static MviReduceResult<TState, TEffect> StateAndEffect<TState, TEffect>(
        TState state,
        TEffect effect)
        where TState : IMviState
        where TEffect : IMviEffect
    {
        return new MviReduceResult<TState, TEffect>(state, new TEffect[] { effect });
    }

    /// <summary>
    /// 创建同时包含状态和多个副作用的规约结果。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effects">副作用集合。</param>
    /// <returns>规约结果。</returns>
    public static MviReduceResult<TState, TEffect> StateAndEffects<TState, TEffect>(
        TState state,
        IReadOnlyList<TEffect> effects)
        where TState : IMviState
        where TEffect : IMviEffect
    {
        return new MviReduceResult<TState, TEffect>(state, effects);
    }
}
