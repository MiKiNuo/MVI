using MiKiNuo.Mvi.Domain.MVI.Reducer;

using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示 MVI 中间件管道。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviMiddlewarePipeline<TState, TIntent, TEffect>(
    IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>> middlewares)
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>> _middlewares = middlewares;

    /// <summary>
    /// 执行中间件管道。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="terminalMiddleware">最终规约委托。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public ValueTask<MviReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        MviMiddlewareStep<TState, TIntent, TEffect> terminalMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(terminalMiddleware);

        MviMiddlewareStep<TState, TIntent, TEffect> currentMiddleware = terminalMiddleware;

        for (int index = _middlewares.Count - 1; index >= 0; index--)
        {
            IMviMiddleware<TState, TIntent, TEffect> middleware = _middlewares[index];
            MviMiddlewareStep<TState, TIntent, TEffect> nextMiddleware = currentMiddleware;

            currentMiddleware = (pipelineContext, token) =>
                middleware.InvokeAsync(pipelineContext, nextMiddleware, token);
        }

        return currentMiddleware(context, cancellationToken);
    }
}
