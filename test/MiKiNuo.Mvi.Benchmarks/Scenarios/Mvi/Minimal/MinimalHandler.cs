using MiKiNuo.Mvi.Runtime.MVI.Intent;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
/// <summary>测量同步 Handler 与零到四次外部委托边界。</summary>
public sealed class MinimalHandler(int externalCalls = 0) : IIntentHandler<MinimalIntent, MinimalState>
{
    /// <summary>获取已完成的外部委托调用数。</summary>
    public int HandledCount { get; private set; }
    /// <summary>提交状态并执行配置的无操作委托。</summary>
    /// <param name="intent">本次最小场景意图。</param>
    /// <param name="context">本次意图的状态提交上下文。</param>
    /// <param name="cancellationToken">调用方取消标记。</param>
    /// <returns>同步完成的处理任务。</returns>
    public ValueTask HandleAsync(MinimalIntent intent, IIntentContext<MinimalState> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(context);

        context.Reduce(new MinimalMutation());
        if (intent is MinimalIntent.EmitNops) HandledCount += externalCalls;
        return ValueTask.CompletedTask;
    }
}
