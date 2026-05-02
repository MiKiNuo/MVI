using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Reducers;

namespace MiKiNuo.Mvi.Core.Dispatching;

/// <summary>
/// 表示由源生成器或手写测试替身提供的意图分发器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图基类类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IIntentDispatcher<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 将意图分发到对应处理器并返回归约结果。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">当前意图。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>归约结果。</returns>
    ValueTask<ReduceResult<TState, TEffect>> DispatchAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken);
}
