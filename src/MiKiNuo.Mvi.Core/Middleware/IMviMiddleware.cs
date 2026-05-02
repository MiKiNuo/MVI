using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Reducers;

namespace MiKiNuo.Mvi.Core.Middleware;

/// <summary>
/// 表示 MVI 中间件。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviMiddleware<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 执行中间件逻辑。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="continuation">继续执行派发管线的委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>归约结果。</returns>
    ValueTask<ReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        MviContinuation<TState, TIntent, TEffect> continuation,
        CancellationToken cancellationToken);
}
