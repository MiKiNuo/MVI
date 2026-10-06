using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
namespace MiKiNuo.Mvi.Runtime.MVI.Middleware;
/// <summary>意图管线中的下一个处理步骤。</summary>
/// <returns>后续处理的完成任务。</returns>
public delegate ValueTask MviNext();
/// <summary>在 Handler 前后执行日志、校验或取消等横切处理。</summary>
/// <typeparam name="TState">组件状态。</typeparam>
/// <typeparam name="TIntent">组件意图。</typeparam>
public interface IMviMiddleware<TState, in TIntent>
    where TState : IMviState where TIntent : IMviIntent
{
    /// <summary>执行一次横切处理；允许拒绝操作，但不允许重复调用 nextStep。</summary>
    /// <param name="intent">输入意图；诊断默认不得记录其值。</param>
    /// <param name="context">本组件状态上下文。</param>
    /// <param name="nextStep">下一步。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理任务。</returns>
    public ValueTask InvokeAsync(TIntent intent, IIntentContext<TState> context, MviNext nextStep, CancellationToken cancellationToken);
}
