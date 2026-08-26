using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Store;

/// <summary>
/// 表示 <see cref="IMviStore{TState, TIntent, TEffect}"/> 的状态路径观察扩展。
/// 可视为 MVI 专用的 WhenAnyValue：基于 <see cref="StatePath{TState, TValue}"/>
/// 从状态源流中投影局部值，并默认去重。
/// </summary>
public static class MviStoreStateExtensions
{
    /// <summary>
    /// 观察单个状态路径，状态快照更新且路径值变化时推送新值。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TIntent">意图类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <typeparam name="TValue">路径终点值类型。</typeparam>
    /// <param name="store">状态存储。</param>
    /// <param name="path">状态访问路径。</param>
    /// <param name="comparer">路径值相等比较器，为 null 时使用默认比较器。</param>
    /// <returns>路径值变化流。</returns>
    public static Observable<TValue> SelectState<TState, TIntent, TEffect, TValue>(
        this IMviStore<TState, TIntent, TEffect> store,
        StatePath<TState, TValue> path,
        IEqualityComparer<TValue>? comparer = null)
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.States
            .Select(path.Getter)
            .DistinctUntilChanged(comparer ?? EqualityComparer<TValue>.Default);
    }

    /// <summary>
    /// 观察两个状态路径，任一路径值变化时用最新值投影结果并去重。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TIntent">意图类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <typeparam name="TValue1">第一个路径终点值类型。</typeparam>
    /// <typeparam name="TValue2">第二个路径终点值类型。</typeparam>
    /// <typeparam name="TResult">投影结果类型。</typeparam>
    /// <param name="store">状态存储。</param>
    /// <param name="path1">第一个状态访问路径。</param>
    /// <param name="path2">第二个状态访问路径。</param>
    /// <param name="selector">投影函数。</param>
    /// <param name="comparer">投影结果相等比较器，为 null 时使用默认比较器。</param>
    /// <returns>投影结果变化流。</returns>
    public static Observable<TResult> SelectState<TState, TIntent, TEffect, TValue1, TValue2, TResult>(
        this IMviStore<TState, TIntent, TEffect> store,
        StatePath<TState, TValue1> path1,
        StatePath<TState, TValue2> path2,
        Func<TValue1, TValue2, TResult> selector,
        IEqualityComparer<TResult>? comparer = null)
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return store.States
            .Select(state => selector(path1.Getter(state), path2.Getter(state)))
            .DistinctUntilChanged(comparer ?? EqualityComparer<TResult>.Default);
    }

    /// <summary>
    /// 观察三个状态路径，任一路径值变化时用最新值投影结果并去重。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TIntent">意图类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <typeparam name="TValue1">第一个路径终点值类型。</typeparam>
    /// <typeparam name="TValue2">第二个路径终点值类型。</typeparam>
    /// <typeparam name="TValue3">第三个路径终点值类型。</typeparam>
    /// <typeparam name="TResult">投影结果类型。</typeparam>
    /// <param name="store">状态存储。</param>
    /// <param name="path1">第一个状态访问路径。</param>
    /// <param name="path2">第二个状态访问路径。</param>
    /// <param name="path3">第三个状态访问路径。</param>
    /// <param name="selector">投影函数。</param>
    /// <param name="comparer">投影结果相等比较器，为 null 时使用默认比较器。</param>
    /// <returns>投影结果变化流。</returns>
    public static Observable<TResult> SelectState<TState, TIntent, TEffect, TValue1, TValue2, TValue3, TResult>(
        this IMviStore<TState, TIntent, TEffect> store,
        StatePath<TState, TValue1> path1,
        StatePath<TState, TValue2> path2,
        StatePath<TState, TValue3> path3,
        Func<TValue1, TValue2, TValue3, TResult> selector,
        IEqualityComparer<TResult>? comparer = null)
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return store.States
            .Select(state => selector(
                path1.Getter(state),
                path2.Getter(state),
                path3.Getter(state)))
            .DistinctUntilChanged(comparer ?? EqualityComparer<TResult>.Default);
    }

    /// <summary>
    /// 观察四个状态路径，任一路径值变化时用最新值投影结果并去重。
    /// </summary>
    /// <typeparam name="TState">状态类型。</typeparam>
    /// <typeparam name="TIntent">意图类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <typeparam name="TValue1">第一个路径终点值类型。</typeparam>
    /// <typeparam name="TValue2">第二个路径终点值类型。</typeparam>
    /// <typeparam name="TValue3">第三个路径终点值类型。</typeparam>
    /// <typeparam name="TValue4">第四个路径终点值类型。</typeparam>
    /// <typeparam name="TResult">投影结果类型。</typeparam>
    /// <param name="store">状态存储。</param>
    /// <param name="path1">第一个状态访问路径。</param>
    /// <param name="path2">第二个状态访问路径。</param>
    /// <param name="path3">第三个状态访问路径。</param>
    /// <param name="path4">第四个状态访问路径。</param>
    /// <param name="selector">投影函数。</param>
    /// <param name="comparer">投影结果相等比较器，为 null 时使用默认比较器。</param>
    /// <returns>投影结果变化流。</returns>
    public static Observable<TResult> SelectState<TState, TIntent, TEffect, TValue1, TValue2, TValue3, TValue4, TResult>(
        this IMviStore<TState, TIntent, TEffect> store,
        StatePath<TState, TValue1> path1,
        StatePath<TState, TValue2> path2,
        StatePath<TState, TValue3> path3,
        StatePath<TState, TValue4> path4,
        Func<TValue1, TValue2, TValue3, TValue4, TResult> selector,
        IEqualityComparer<TResult>? comparer = null)
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        return store.States
            .Select(state => selector(
                path1.Getter(state),
                path2.Getter(state),
                path3.Getter(state),
                path4.Getter(state)))
            .DistinctUntilChanged(comparer ?? EqualityComparer<TResult>.Default);
    }
}
