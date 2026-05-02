using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Reducers;

namespace MiKiNuo.Mvi.Core.Dispatching;

/// <summary>
/// 表示 MVI 意图处理器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">具体意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IIntentHandler<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 根据当前状态和意图创建归约结果。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">当前意图。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>归约结果。</returns>
    ValueTask<ReduceResult<TState, TEffect>> HandleAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken);
}
