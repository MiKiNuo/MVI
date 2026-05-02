using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Dispatching;
using MiKiNuo.Mvi.Core.Middleware;
using MiKiNuo.Mvi.Core.Reducers;
using MiKiNuo.Mvi.Core.Store;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviStoreTests
{
    [Test]
    public async Task DispatchAsync_publishes_initial_state_reduced_state_and_effects_in_order()
    {
        var initialState = new CounterState(0);
        var effect = new CounterEffect("incremented");
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) => ValueTask.FromResult(
                ReduceResults.WithEffect(new CounterState(state.Count + intent.Amount), effect)));
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(initialState, dispatcher);
        var observedStates = new List<CounterState>();
        var observedEffects = new List<CounterEffect>();

        using var stateSubscription = store.States.Subscribe(observedStates.Add);
        using var effectSubscription = store.Effects.Subscribe(observedEffects.Add);

        await store.DispatchAsync(new CounterIntent(1, 2), CancellationToken.None);

        await Assert.That(observedStates).IsEquivalentTo(new[]
        {
            initialState,
            new CounterState(2),
        });
        await Assert.That(observedEffects).IsEquivalentTo(new[] { effect });
    }

    [Test]
    public async Task DispatchAsync_invokes_middleware_with_current_state_and_intent()
    {
        var initialState = new CounterState(7);
        var intent = new CounterIntent(2, 3);
        var observed = new List<string>();
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, dispatchedIntent, _) => ValueTask.FromResult(
                ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + dispatchedIntent.Amount))));
        var middleware = new DelegateMiddleware<CounterState, CounterIntent, CounterEffect>(
            async (context, continuation, cancellationToken) =>
            {
                observed.Add($"{context.State.Count}:{context.Intent.Amount}");
                return await continuation(context, cancellationToken);
            });
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(
            initialState,
            dispatcher,
            new[] { middleware });

        await store.DispatchAsync(intent, CancellationToken.None);

        await Assert.That(observed).IsEquivalentTo(new[] { "7:3" });
    }

    [Test]
    public async Task DispatchAsync_allows_middleware_to_transform_result_before_publish()
    {
        var initialState = new CounterState(1);
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) => ValueTask.FromResult(
                ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + intent.Amount))));
        var middleware = new DelegateMiddleware<CounterState, CounterIntent, CounterEffect>(
            async (context, continuation, cancellationToken) =>
            {
                var result = await continuation(context, cancellationToken);
                return ReduceResults.WithEffect(new CounterState(result.State.Count * 10), new CounterEffect("middleware"));
            });
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(
            initialState,
            dispatcher,
            new[] { middleware });
        var observedStates = new List<CounterState>();
        var observedEffects = new List<CounterEffect>();

        using var stateSubscription = store.States.Subscribe(observedStates.Add);
        using var effectSubscription = store.Effects.Subscribe(observedEffects.Add);

        await store.DispatchAsync(new CounterIntent(3, 2), CancellationToken.None);

        await Assert.That(observedStates).IsEquivalentTo(new[]
        {
            initialState,
            new CounterState(30),
        });
        await Assert.That(observedEffects).IsEquivalentTo(new[] { new CounterEffect("middleware") });
    }

    [Test]
    public async Task DispatchAsync_propagates_dispatcher_exception_without_publishing_partial_result()
    {
        var initialState = new CounterState(10);
        var failure = new InvalidOperationException("dispatcher failed");
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (_, _, _) => throw failure);
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(initialState, dispatcher);
        var observedStates = new List<CounterState>();
        var observedEffects = new List<CounterEffect>();

        using var stateSubscription = store.States.Subscribe(observedStates.Add);
        using var effectSubscription = store.Effects.Subscribe(observedEffects.Add);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.DispatchAsync(new CounterIntent(4, 5), CancellationToken.None).AsTask());

        await Assert.That(exception).IsSameReferenceAs(failure);
        await Assert.That(observedStates).IsEquivalentTo(new[] { initialState });
        await Assert.That(observedEffects).IsEmpty();
    }

    [Test]
    public async Task DispatchAsync_honors_pre_canceled_token_before_dispatcher_runs()
    {
        var initialState = new CounterState(20);
        var dispatcherWasCalled = false;
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) =>
            {
                dispatcherWasCalled = true;
                return ValueTask.FromResult(
                    ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + intent.Amount)));
            });
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(initialState, dispatcher);
        var observedStates = new List<CounterState>();
        var observedEffects = new List<CounterEffect>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        using var stateSubscription = store.States.Subscribe(observedStates.Add);
        using var effectSubscription = store.Effects.Subscribe(observedEffects.Add);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.DispatchAsync(new CounterIntent(5, 6), cancellation.Token).AsTask());

        await Assert.That(dispatcherWasCalled).IsFalse();
        await Assert.That(observedStates).IsEquivalentTo(new[] { initialState });
        await Assert.That(observedEffects).IsEmpty();
    }

    private sealed record CounterState(int Count) : IMviState;

    private sealed record CounterIntent(int Kind, int Amount) : IMviIntent;

    private sealed record CounterEffect(string Message) : IMviEffect;

    private sealed class DelegateIntentDispatcher<TState, TIntent, TEffect>
        : IIntentDispatcher<TState, TIntent, TEffect>
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        private readonly Func<TState, TIntent, CancellationToken, ValueTask<ReduceResult<TState, TEffect>>> dispatchAsync;

        public DelegateIntentDispatcher(
            Func<TState, TIntent, CancellationToken, ValueTask<ReduceResult<TState, TEffect>>> dispatchAsync)
        {
            this.dispatchAsync = dispatchAsync;
        }

        public ValueTask<ReduceResult<TState, TEffect>> DispatchAsync(
            TState state,
            TIntent intent,
            CancellationToken cancellationToken)
        {
            return dispatchAsync(state, intent, cancellationToken);
        }
    }

    private sealed class DelegateMiddleware<TState, TIntent, TEffect>
        : IMviMiddleware<TState, TIntent, TEffect>
        where TState : IMviState
        where TIntent : IMviIntent
        where TEffect : IMviEffect
    {
        private readonly Func<
            MviMiddlewareContext<TState, TIntent, TEffect>,
            MviContinuation<TState, TIntent, TEffect>,
            CancellationToken,
            ValueTask<ReduceResult<TState, TEffect>>> invokeAsync;

        public DelegateMiddleware(
            Func<
                MviMiddlewareContext<TState, TIntent, TEffect>,
                MviContinuation<TState, TIntent, TEffect>,
                CancellationToken,
                ValueTask<ReduceResult<TState, TEffect>>> invokeAsync)
        {
            this.invokeAsync = invokeAsync;
        }

        public ValueTask<ReduceResult<TState, TEffect>> InvokeAsync(
            MviMiddlewareContext<TState, TIntent, TEffect> context,
            MviContinuation<TState, TIntent, TEffect> continuation,
            CancellationToken cancellationToken)
        {
            return invokeAsync(context, continuation, cancellationToken);
        }
    }
}
