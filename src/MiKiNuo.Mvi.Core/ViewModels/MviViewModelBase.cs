using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Store;
using R3;

namespace MiKiNuo.Mvi.Core.ViewModels;

/// <summary>
/// 表示 MVI ViewModel 基类。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public abstract class MviViewModelBase<TState, TIntent, TEffect> : MviObservableObject, IDisposable
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly IMviStore<TState, TIntent, TEffect> store;
    private readonly IDisposable stateSubscription;

    /// <summary>
    /// 初始化 MVI ViewModel 基类。
    /// </summary>
    /// <param name="store">MVI 状态容器。</param>
    protected MviViewModelBase(IMviStore<TState, TIntent, TEffect> store)
    {
        this.store = store;
        stateSubscription = store.States.Subscribe(ApplyStateCore);
    }

    /// <summary>
    /// 派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <returns>异步任务。</returns>
    protected ValueTask DispatchAsync(TIntent intent)
    {
        return store.DispatchAsync(intent, CancellationToken.None);
    }

    /// <summary>
    /// 派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    protected ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken)
    {
        return store.DispatchAsync(intent, cancellationToken);
    }

    /// <summary>
    /// 将状态应用到 ViewModel。
    /// </summary>
    /// <param name="state">状态。</param>
    protected abstract void ApplyStateCore(TState state);

    /// <inheritdoc />
    public void Dispose()
    {
        stateSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
