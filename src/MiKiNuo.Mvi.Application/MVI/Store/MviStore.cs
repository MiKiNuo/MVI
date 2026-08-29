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
/// 数据流：Intent → Middleware → Reduce → IntentHandler → 可选后续 Intent 的 Reduce → EffectDispatcher。
/// 每个 Intent 只规约一次，Store 负责合并完整的副作用序列。
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
    private readonly IMviIntentHandler<TState, TIntent> _intentHandler;
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
        IMviIntentHandler<TState, TIntent> intentHandler,
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
    /// 派发意图并执行异步业务与可选的后续意图。
    /// </summary>
    /// <remarks>
    /// 派发门（SemaphoreSlim）只保护 Reduce 管线与状态更新；
    /// 副作用在门释放后统一派发，因此 EffectDispatcher 可以安全地向
    /// 同一 Store 再派发后续 Intent（重入安全），不会形成死锁。
    /// </remarks>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(intent);

        MviReduceResult<TState, TEffect> finalResult;

        await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);

            finalResult = await _pipeline.InvokeAsync(
                context,
                ExecuteCoreAsync,
                cancellationToken).ConfigureAwait(false);

            _state.Value = finalResult.State;
        }
        finally
        {
            _ = _dispatchGate.Release();
        }

        await DispatchEffectsAsync(finalResult.Effects, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// 执行核心调度并返回包含完整副作用序列的最终规约结果。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>最终规约结果。</returns>
    private async ValueTask<MviReduceResult<TState, TEffect>> ExecuteCoreAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        TState state = context.State;
        TIntent intent = context.Intent;

        MviReduceResult<TState, TEffect> initial = _reducer.Reduce(state, intent);

        // 立即发布初始状态，让异步处理器和状态观察者看到处理中状态。
        _state.Value = initial.State;

        TIntent? followUpIntent = await _intentHandler
            .HandleAsync(initial.State, intent, cancellationToken)
            .ConfigureAwait(false);

        if (followUpIntent is null)
        {
            return initial;
        }

        MviReduceResult<TState, TEffect> followUp = _reducer.Reduce(initial.State, followUpIntent);
        if (initial.Effects.Count == 0)
        {
            return followUp;
        }

        if (followUp.Effects.Count == 0)
        {
            return new MviReduceResult<TState, TEffect>(followUp.State, initial.Effects);
        }

        TEffect[] effects = new TEffect[initial.Effects.Count + followUp.Effects.Count];
        for (int i = 0; i < initial.Effects.Count; i++)
        {
            effects[i] = initial.Effects[i];
        }

        for (int i = 0; i < followUp.Effects.Count; i++)
        {
            effects[initial.Effects.Count + i] = followUp.Effects[i];
        }

        return new MviReduceResult<TState, TEffect>(followUp.State, effects);
    }

    /// <summary>
    /// 派发副作用集合到分发器。
    /// </summary>
    /// <param name="effects">副作用集合。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask DispatchEffectsAsync(
        IReadOnlyList<TEffect> effects,
        CancellationToken cancellationToken)
    {
        foreach (TEffect effect in effects)
        {
            _effects.OnNext(effect);
            await _effectDispatcher.DispatchAsync(effect, cancellationToken).ConfigureAwait(false);
        }
    }
}
