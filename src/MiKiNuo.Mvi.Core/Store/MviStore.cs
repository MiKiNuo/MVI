using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Dispatching;
using MiKiNuo.Mvi.Core.Middleware;
using MiKiNuo.Mvi.Core.Reducers;
using R3;

namespace MiKiNuo.Mvi.Core.Store;

/// <summary>
/// 表示默认 MVI 状态容器实现。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class MviStore<TState, TIntent, TEffect> : IMviStore<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private readonly MviMiddlewarePipeline<TState, TIntent, TEffect> pipeline;
    private readonly BehaviorSubject<TState> states;
    private readonly Subject<TEffect> effects;
    private TState currentState;

    /// <summary>
    /// 初始化默认 MVI 状态容器。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="dispatcher">意图分发器。</param>
    public MviStore(
        TState initialState,
        IIntentDispatcher<TState, TIntent, TEffect> dispatcher)
        : this(initialState, dispatcher, Array.Empty<IMviMiddleware<TState, TIntent, TEffect>>())
    {
    }

    /// <summary>
    /// 初始化默认 MVI 状态容器。
    /// </summary>
    /// <param name="initialState">初始状态。</param>
    /// <param name="dispatcher">意图分发器。</param>
    /// <param name="middleware">中间件集合。</param>
    public MviStore(
        TState initialState,
        IIntentDispatcher<TState, TIntent, TEffect> dispatcher,
        IReadOnlyList<IMviMiddleware<TState, TIntent, TEffect>> middleware)
    {
        currentState = initialState;
        pipeline = new MviMiddlewarePipeline<TState, TIntent, TEffect>(dispatcher, middleware);
        states = new BehaviorSubject<TState>(initialState);
        effects = new Subject<TEffect>();
    }

    /// <inheritdoc />
    public Observable<TState> States => states;

    /// <inheritdoc />
    public Observable<TEffect> Effects => effects;

    /// <inheritdoc />
    public async ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = new MviMiddlewareContext<TState, TIntent, TEffect>(currentState, intent);
        ReduceResult<TState, TEffect> result = await pipeline.InvokeAsync(
            context,
            cancellationToken).ConfigureAwait(false);

        currentState = result.State;
        states.OnNext(result.State);

        foreach (var effect in result.Effects)
        {
            effects.OnNext(effect);
        }
    }
}
