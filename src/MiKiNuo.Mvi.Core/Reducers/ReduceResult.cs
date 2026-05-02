namespace MiKiNuo.Mvi.Core.Reducers;

/// <summary>
/// 表示状态归约结果。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="State">新的状态。</param>
/// <param name="Effects">需要提交的副作用集合。</param>
public readonly record struct ReduceResult<TState, TEffect>(
    TState State,
    IReadOnlyList<TEffect> Effects);
