using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Reducers;

namespace MiKiNuo.Mvi.Core.Middleware;

/// <summary>
/// 表示继续执行 MVI 派发管线的委托。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="context">中间件上下文。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>归约结果。</returns>
public delegate ValueTask<ReduceResult<TState, TEffect>> MviContinuation<TState, TIntent, TEffect>(
    MviMiddlewareContext<TState, TIntent, TEffect> context,
    CancellationToken cancellationToken)
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect;
