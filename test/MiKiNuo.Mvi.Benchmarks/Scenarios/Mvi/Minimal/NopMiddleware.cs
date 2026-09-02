using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

/// <summary>
/// 表示无操作直通中间件：仅计数并调用下一层，用于测量中间件链每层的固定开销。
/// </summary>
public sealed class NopMiddleware
    : IMviMiddleware<MinimalState, MinimalIntent, MinimalEffect>
{
    /// <summary>
    /// 获取被调用的总次数。
    /// </summary>
    public int InvocationCount { get; private set; }

    /// <summary>
    /// 直通执行：计数后调用下一层中间件。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一层中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>下一层产出的规约结果。</returns>
    public async ValueTask<MviReduceResult<MinimalState, MinimalEffect>> InvokeAsync(
        MviMiddlewareContext<MinimalState, MinimalIntent, MinimalEffect> context,
        MviMiddlewareStep<MinimalState, MinimalIntent, MinimalEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);

        InvocationCount++;
        return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
    }
}
