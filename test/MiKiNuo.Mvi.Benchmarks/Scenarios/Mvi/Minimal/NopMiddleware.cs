using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
/// <summary>用于测量中间件链固定开销。</summary>
public sealed class NopMiddleware : IMviMiddleware<MinimalState, MinimalIntent>
{
    /// <summary>获取调用次数。</summary>
    public int InvocationCount { get; private set; }
    /// <summary>调用下一层，不创建副作用描述对象。</summary>
    /// <param name="intent">本次最小场景意图。</param>
    /// <param name="context">本次意图的状态提交上下文。</param>
    /// <param name="nextStep">下一层处理委托。</param>
    /// <param name="cancellationToken">调用方取消标记。</param>
    /// <returns>下一层的处理任务。</returns>
    public ValueTask InvokeAsync(MinimalIntent intent, IIntentContext<MinimalState> context, MviNext nextStep, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        InvocationCount++;
        return nextStep();
    }
}
