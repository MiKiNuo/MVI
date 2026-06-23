using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.IntentHandler;

/// <summary>
/// 表示意图处理器，执行业务逻辑产生变更。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TMutation">变更类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviIntentHandler<TState, TIntent, TMutation, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TMutation : IMviMutation
    where TEffect : IMviEffect
{
    /// <summary>
    /// 处理意图并产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<TMutation, TEffect>> HandleAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken = default);
}
