using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.IntentHandler;

/// <summary>
/// 表示意图处理器,执行异步业务并产生后续意图。
/// </summary>
/// <remarks>
/// IntentHandler 承担异步业务调用与持久化写入等工作，
/// 产生的后续意图由 Store 传递给 Reducer，完成后续状态转换与副作用产出。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
public interface IMviIntentHandler<TState, TIntent>
    where TState : IMviState
    where TIntent : IMviIntent
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图,无后续工作时返回 null。</returns>
    public ValueTask<TIntent?> HandleAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken = default);
}
