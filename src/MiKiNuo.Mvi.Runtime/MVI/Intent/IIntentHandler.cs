using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Runtime.MVI.Intent;
/// <summary>处理业务意图；通过上下文提交变化，通过注入的服务执行外部操作。</summary>
/// <typeparam name="TIntent">处理的意图类型。</typeparam>
/// <typeparam name="TState">所属组件状态。</typeparam>
public interface IIntentHandler<in TIntent, TState>
    where TIntent : IMviIntent where TState : IMviState
{
    /// <summary>处理一次已经准入的业务操作；完成返回后上下文即失效。</summary>
    /// <param name="intent">交互发生时捕获的意图快照。</param>
    /// <param name="context">仅允许修改本组件状态的上下文。</param>
    /// <param name="cancellationToken">调用方和组件生命周期联合取消标记。</param>
    /// <returns>该意图的完整处理任务，包括显式等待的外部调用。</returns>
    public ValueTask HandleAsync(TIntent intent, IIntentContext<TState> context, CancellationToken cancellationToken);
}
