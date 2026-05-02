using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Store;

namespace MiKiNuo.Mvi.Core.Effects;

/// <summary>
/// 表示将后续意图派发回状态容器的意图接收器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class StoreIntentSink<TState, TIntent, TEffect> : IIntentSink<TIntent>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IMviStore<TState, TIntent, TEffect> store;

    /// <summary>
    /// 初始化状态容器意图接收器。
    /// </summary>
    /// <param name="store">状态容器。</param>
    public StoreIntentSink(IMviStore<TState, TIntent, TEffect> store)
    {
        this.store = store;
    }

    /// <inheritdoc />
    public ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken)
    {
        return store.DispatchAsync(intent, cancellationToken);
    }
}
