using MiKiNuo.Mvi.Abstractions;
using R3;

namespace MiKiNuo.Mvi.Core.Store;

/// <summary>
/// 表示 MVI 状态容器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviStore<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 获取状态流。
    /// </summary>
    Observable<TState> States { get; }

    /// <summary>
    /// 获取副作用流。
    /// </summary>
    Observable<TEffect> Effects { get; }

    /// <summary>
    /// 派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken);
}
