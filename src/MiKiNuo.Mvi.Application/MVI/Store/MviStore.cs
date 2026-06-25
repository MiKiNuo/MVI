using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;

namespace MiKiNuo.Mvi.Application.MVI.Store;

/// <summary>
/// 表示经典 MVI 状态存储。
/// </summary>
/// <remarks>
/// 数据流：Intent → Middleware → IntentHandler(异步) → Reducer(纯函数) → 新 State + Effects → EffectDispatcher。
/// </remarks>
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
    private readonly IMviIntentHandler<TState, TIntent, TEffect> _intentHandler;
    private readonly IMviReducer<TState, TIntent, TEffect> _reducer;
    private readonly IMviEffectDispatcher<TEffect> _effectDispatcher;
    private readonly MviMiddlewarePipeline<TState, TIntent, TEffect> _pipeline;
    private readonly SemaphoreSlim _dispatchGate;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI 状态存储。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="intentHandler">意图处理器。</param>
    /// <param name="reducer">规约器。</param>
    /// <param name="effectDispatcher">副作用分发器。</param>
    /// <param name="middlewares">中间件集合。</param>
    public MviStore(
        TState initialState,
        IMviIntentHandler<TState, TIntent, TEffect> intentHandler,
        IMviReducer<TState, TIntent, TEffect> reducer,
        IMviEffectDispatcher<TEffect> effectDispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>>? middlewares = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(effectDispatcher);

        _state = new ReactiveProperty<TState>(initialState);
        _effects = new Subject<TEffect>();
        _intentHandler = intentHandler;
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
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<TEffect> effects;

        await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);
            MviReduceResult<TState, TEffect> result = await _pipeline.InvokeAsync(
                context, HandleAndReduceCoreAsync, cancellationToken).ConfigureAwait(false);

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

    private async ValueTask<MviReduceResult<TState, TEffect>> HandleAndReduceCoreAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // IntentHandler：异步业务逻辑，产生动作副作用。
        IReadOnlyList<TEffect> actionEffects = await _intentHandler
            .HandleAsync(context.State, context.Intent, cancellationToken)
            .ConfigureAwait(false);

        // Reducer：纯函数 (State, Intent) → (新 State, 派生副作用)。
        MviReduceResult<TState, TEffect> reduceResult = _reducer.Reduce(context.State, context.Intent);

        // 合并动作副作用与派生副作用。
        List<TEffect> allEffects = new(actionEffects.Count + reduceResult.Effects.Count);
        allEffects.AddRange(actionEffects);
        allEffects.AddRange(reduceResult.Effects);

        return new MviReduceResult<TState, TEffect>(reduceResult.State, allEffects);
    }
}
