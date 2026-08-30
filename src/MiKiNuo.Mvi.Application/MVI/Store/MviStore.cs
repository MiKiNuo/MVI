using MiKiNuo.Mvi.Application.MVI.Effect;
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
/// 数据流：Intent → Middleware → Reduce → State 发布 → EffectDispatcher（锁外顺序派发）。
/// EffectDispatcher 执行副作用后可回流新 Intent，回流 Intent 作为普通派发重新进入管线，
/// 中间件全程可见。Reducer 是唯一决策点：是否发起异步调用由它产出的 Effect 表达。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviStore<TState, TIntent, TEffect>
    : IMviStore<TState, TIntent, TEffect>, IMviIntentSink<TIntent>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly ReactiveProperty<TState> _state;
    private readonly IMviReducer<TState, TIntent, TEffect> _reducer;
    private readonly IMviEffectDispatcher<TEffect> _effectDispatcher;
    private readonly MviMiddlewarePipeline<TState, TIntent, TEffect> _pipeline;
    private readonly SemaphoreSlim _dispatchGate;
    private bool _isDisposed;

    /// <summary>
    /// 初始化 MVI 状态存储。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="reducer">规约器。</param>
    /// <param name="effectDispatcher">副作用分发器。</param>
    /// <param name="middlewares">中间件集合。</param>
    public MviStore(
        TState initialState,
        IMviReducer<TState, TIntent, TEffect> reducer,
        IMviEffectDispatcher<TEffect> effectDispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>>? middlewares = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(effectDispatcher);

        _state = new ReactiveProperty<TState>(initialState);
        _reducer = reducer;
        _effectDispatcher = effectDispatcher;
        _pipeline = new MviMiddlewarePipeline<TState, TIntent, TEffect>(middlewares ?? []);
        _dispatchGate = new SemaphoreSlim(1, 1);

        if (effectDispatcher is IMviIntentSinkAttachable<TIntent> attachable)
        {
            attachable.Attach(this);
        }
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
    /// 派发意图：锁内完成 Middleware → Reduce → State 原子发布，锁外顺序派发副作用。
    /// </summary>
    /// <remarks>
    /// 派发门（SemaphoreSlim）只保护同步的规约管线与状态更新；
    /// 副作用在门释放后统一派发，因此 EffectDispatcher 可以安全地向
    /// 同一 Store 回流后续 Intent（重入安全），不会形成死锁，
    /// 慢副作用也不会阻塞同 Store 的其他 Intent。
    /// </remarks>
    /// <param name="intent">意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(intent);

        MviReduceResult<TState, TEffect> result;

        await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            MviMiddlewareContext<TState, TIntent, TEffect> context = new(CurrentState, intent);

            result = await _pipeline.InvokeAsync(
                context,
                ExecuteReduceCore,
                cancellationToken).ConfigureAwait(false);

            _state.Value = result.State;
        }
        finally
        {
            _ = _dispatchGate.Release();
        }

        await DispatchEffectsAsync(result.Effects, cancellationToken).ConfigureAwait(false);
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
        _dispatchGate.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 执行同步规约，是中间件管线的终端步骤。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    private ValueTask<MviReduceResult<TState, TEffect>> ExecuteReduceCore(
        MviMiddlewareContext<TState, TIntent, TEffect> context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_reducer.Reduce(context.State, context.Intent));
    }

    /// <summary>
    /// 在锁外按序派发副作用集合到分发器。
    /// </summary>
    /// <param name="effects">副作用集合。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask DispatchEffectsAsync(
        IReadOnlyList<TEffect> effects,
        CancellationToken cancellationToken)
    {
        foreach (TEffect effect in effects)
        {
            await _effectDispatcher.DispatchAsync(effect, cancellationToken).ConfigureAwait(false);
        }
    }
}
