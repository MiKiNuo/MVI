using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
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
/// 数据流：Intent → Middleware → 第一次 Reduce(产中间状态) → IntentHandler(异步业务,产业务结果) → 第二次 Reduce(消费业务结果,产最终状态+Effects) → EffectDispatcher。
/// 无异步业务的 Intent 仅执行第一次 Reduce。
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
    /// 派发意图,执行两次 Reduce 与异步业务。
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

        List<TEffect> pendingEffects = new();

        await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);

            MviReduceResult<TState, TEffect> finalResult = await _pipeline.InvokeAsync(
                context,
                (pipelineContext, token) => ExecuteCoreAsync(pipelineContext, pendingEffects, token),
                cancellationToken).ConfigureAwait(false);

            _state.Value = finalResult.State;
            pendingEffects.AddRange(finalResult.Effects);
        }
        finally
        {
            _ = _dispatchGate.Release();
        }

        await DispatchEffectsAsync(pendingEffects, cancellationToken).ConfigureAwait(false);
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
    /// 执行核心调度:两次 Reduce 与异步业务。
    /// 副作用不直接派发，统一收集到 <paramref name="pendingEffects"/>，
    /// 由 <see cref="DispatchAsync"/> 在派发门释放后统一派发。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="pendingEffects">待派发副作用收集器。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>最终规约结果。</returns>
    private async ValueTask<MviReduceResult<TState, TEffect>> ExecuteCoreAsync(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        List<TEffect> pendingEffects,
        CancellationToken cancellationToken)
    {
        TState state = context.State;
        TIntent intent = context.Intent;

        // 第一次 Reduce:无业务结果,产中间状态(如 IsBusy)
        MviReduceResult<TState, TEffect> intermediate = _reducer.Reduce(state, intent, null);

        // 立即更新中间状态,让 IntentHandler 看到中间状态
        _state.Value = intermediate.State;
        pendingEffects.AddRange(intermediate.Effects);

        // 异步业务
        IMviBusinessResult? businessResult = await _intentHandler
            .HandleAsync(intermediate.State, intent, cancellationToken)
            .ConfigureAwait(false);

        // 无业务结果:中间状态即最终状态,返回空 Effects 避免外层重复派发
        if (businessResult is null)
        {
            return MviReduceResult.State<TState, TEffect>(intermediate.State);
        }

        // 第二次 Reduce:有业务结果,产最终状态与副作用
        return _reducer.Reduce(intermediate.State, intent, businessResult);
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
