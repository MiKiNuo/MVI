namespace MiKiNuo.Mvi.Core.Reducers;

/// <summary>
/// 提供状态归约结果的创建方法。
/// </summary>
public static class ReduceResults
{
    /// <summary>
    /// 创建无副作用的归约结果。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">新的状态。</param>
    /// <returns>状态归约结果。</returns>
    public static ReduceResult<TState, TEffect> StateOnly<TState, TEffect>(TState state)
    {
        return new ReduceResult<TState, TEffect>(state, Array.Empty<TEffect>());
    }

    /// <summary>
    /// 创建包含单个副作用的归约结果。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">新的状态。</param>
    /// <param name="effect">需要提交的副作用。</param>
    /// <returns>状态归约结果。</returns>
    public static ReduceResult<TState, TEffect> WithEffect<TState, TEffect>(
        TState state,
        TEffect effect)
    {
        return new ReduceResult<TState, TEffect>(state, new[] { effect });
    }

    /// <summary>
    /// 创建包含多个副作用的归约结果。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="state">新的状态。</param>
    /// <param name="effects">需要提交的副作用集合。</param>
    /// <returns>状态归约结果。</returns>
    public static ReduceResult<TState, TEffect> WithEffects<TState, TEffect>(
        TState state,
        IReadOnlyList<TEffect> effects)
    {
        return new ReduceResult<TState, TEffect>(state, effects.ToArray());
    }
}
