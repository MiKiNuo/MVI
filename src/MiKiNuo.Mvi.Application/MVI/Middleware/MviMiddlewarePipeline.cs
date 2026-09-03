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
/// <remarks>
/// 构造期把中间件集合快照为数组，并与终端规约步骤一次性组合出完整调用链；
/// 每次派发零重建成本地复用同一条链。
/// 构造之后对传入集合的变更不再影响管线。
/// </remarks>
public sealed class MviMiddlewarePipeline<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly MviMiddlewareStep<TState, TIntent, TEffect> _composedStep;

    /// <summary>
    /// 初始化 MVI 中间件管道：快照中间件集合并预组合完整调用链。
    /// </summary>
    /// <param name="middlewares">中间件集合，允许为空；构造后对集合的变更不影响管线。</param>
    /// <param name="terminalMiddleware">终端规约委托，通常是 Store 的规约核心。</param>
    /// <exception cref="ArgumentNullException">终端委托为 null 时抛出。</exception>
    public MviMiddlewarePipeline(
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>>? middlewares,
        MviMiddlewareStep<TState, TIntent, TEffect> terminalMiddleware)
    {
        ArgumentNullException.ThrowIfNull(terminalMiddleware);

        IMviMiddleware<TState, TIntent, TEffect>[] snapshot = middlewares is null
            ? Array.Empty<IMviMiddleware<TState, TIntent, TEffect>>()
            : middlewares.ToArray();

        MviMiddlewareStep<TState, TIntent, TEffect> composed = terminalMiddleware;
        for (int index = snapshot.Length - 1; index >= 0; index--)
        {
            IMviMiddleware<TState, TIntent, TEffect> middleware = snapshot[index];
            MviMiddlewareStep<TState, TIntent, TEffect> next = composed;

            composed = async (pipelineContext, token) =>
                await middleware.InvokeAsync(pipelineContext, next, token).ConfigureAwait(false);
        }

        _composedStep = composed;
    }

    /// <summary>
    /// 执行预组合的中间件管道。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public ValueTask<MviReduceResult<TState, TEffect>> InvokeAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _composedStep(context, cancellationToken);
    }
}
