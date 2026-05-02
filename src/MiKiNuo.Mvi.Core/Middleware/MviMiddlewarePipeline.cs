using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Dispatching;
using MiKiNuo.Mvi.Core.Reducers;

namespace MiKiNuo.Mvi.Core.Middleware;

/// <summary>
/// 表示 MVI 中间件管线。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviMiddlewarePipeline<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>> middleware;
    private readonly IIntentDispatcher<TState, TIntent, TEffect> dispatcher;

    /// <summary>
    /// 初始化 MVI 中间件管线。
    /// </summary>
    /// <param name="dispatcher">意图分发器。</param>
    /// <param name="middleware">中间件集合。</param>
    public MviMiddlewarePipeline(
        IIntentDispatcher<TState, TIntent, TEffect> dispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>> middleware)
    {
        this.dispatcher = dispatcher;
        this.middleware = middleware;
    }

    /// <summary>
    /// 执行中间件管线。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>归约结果。</returns>
    public ValueTask<ReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        return InvokeAtAsync(0, context, cancellationToken);
    }

    private ValueTask<ReduceResult<TState, TEffect>> InvokeAtAsync(
        int index,
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        if (index >= middleware.Count)
        {
            return dispatcher.DispatchAsync(context.State, context.Intent, cancellationToken);
        }

        return middleware[index].InvokeAsync(
            context,
            (nextContext, nextCancellationToken) => InvokeAtAsync(index + 1, nextContext, nextCancellationToken),
            cancellationToken);
    }
}
