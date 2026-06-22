using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using R3;

using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.Store;

/// <summary>
/// 表示 R3 驱动的 MVI 状态存储。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviStore<TState, TIntent, TEffect> : IMviStore<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly ReactiveProperty<TState> _state;
    private readonly Subject<TEffect> _effects;
    private readonly IMviReducer<TState, TIntent, TEffect> _reducer;
    private readonly IMviEffectDispatcher<TEffect> _effectDispatcher;
    private readonly MviMiddlewarePipeline<TState, TIntent, TEffect> _pipeline;
    private readonly SemaphoreSlim _dispatchGate;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 R3 驱动的 MVI 状态存储。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="reducer">状态规约器。</param>
    /// <param name="effectDispatcher">副作用分发器。</param>
    /// <param name="middlewares">中间件集合。</param>
    public MviStore(
        TState initialState,
        IMviReducer<TState, TIntent, TEffect> reducer,
        IMviEffectDispatcher<TEffect> effectDispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>>? middlewares = null)
    {
        _state = new ReactiveProperty<TState>(initialState);
        _effects = new Subject<TEffect>();
        _reducer = reducer;
        _effectDispatcher = effectDispatcher;
        _pipeline = new MviMiddlewarePipeline<TState, TIntent, TEffect>(middlewares ?? []);
        _dispatchGate = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// 获取当前状态。
    /// </summary>
    public TState CurrentState => _state.Value;

    /// <summary>
    /// 获取状态变化流。
    /// </summary>
    public Observable<TState> States => _state;

    /// <summary>
    /// 获取副作用变化流。
    /// </summary>
    public Observable<TEffect> Effects => _effects;

    /// <summary>
    /// 派发意图。
    /// </summary>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        IReadOnlyList<TEffect> effects;

        await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);
            MviReduceResult<TState, TEffect> result = await _pipeline.InvokeAsync(
                context,
                ReduceCoreAsync,
                cancellationToken).ConfigureAwait(false);

            _state.Value = result.State;
            effects = result.Effects;
        }
        finally
        {
            _ = _dispatchGate.Release();
        }

        foreach (TEffect effect in effects)
        {
            _effects.OnNext(effect);
            await _effectDispatcher.DispatchAsync(effect, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 释放所有资源。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _state.Dispose();
        _effects.Dispose();
        _dispatchGate.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private ValueTask<MviReduceResult<TState, TEffect>> ReduceCoreAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MviReduceResult<TState, TEffect> result = _reducer.Reduce(context.State, context.Intent);
        return ValueTask.FromResult(result);
    }
}
