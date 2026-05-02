using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Dispatching;
using MiKiNuo.Mvi.Core.Effects;
using MiKiNuo.Mvi.Core.Reducers;
using MiKiNuo.Mvi.Core.Store;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviEffectRuntimeTests
{
    [Test]
    public async Task HandleAsync_routes_emitted_effect_to_registered_handler()
    {
        var emittedEffect = new CounterEffect("handled");
        var handledEffects = new List<CounterEffect>();
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) => ValueTask.FromResult(
                ReduceResults.WithEffect(new CounterState(state.Count + intent.Amount), emittedEffect)));
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(new CounterState(0), dispatcher);
        var runtime = new MviEffectRuntime<CounterIntent>(
            new NullIntentSink<CounterIntent>(),
            new IEffectHandler<CounterEffect, CounterIntent>[]
            {
                new DelegateEffectHandler<CounterEffect, CounterIntent>((effect, _, _) =>
                {
                    handledEffects.Add(effect);
                    return ValueTask.CompletedTask;
                }),
            });

        using var effectSubscription = store.Effects.Subscribe(effect =>
            runtime.HandleAsync(effect, CancellationToken.None).AsTask().GetAwaiter().GetResult());

        await store.DispatchAsync(new CounterIntent(1, 2), CancellationToken.None);

        await Assert.That(handledEffects).IsEquivalentTo(new[] { emittedEffect });
    }

    [Test]
    public async Task HandleAsync_allows_effect_handler_to_dispatch_follow_up_intent()
    {
        var emittedEffect = new CounterEffect("follow-up");
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) => ValueTask.FromResult(
                ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + intent.Amount))));
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(new CounterState(0), dispatcher);
        var runtime = new MviEffectRuntime<CounterIntent>(
            new StoreIntentSink<CounterState, CounterIntent, CounterEffect>(store),
            new IEffectHandler<CounterEffect, CounterIntent>[]
            {
                new DelegateEffectHandler<CounterEffect, CounterIntent>((_, sink, cancellationToken) =>
                    sink.DispatchAsync(new CounterIntent(2, 5), cancellationToken)),
            });
        var observedStates = new List<CounterState>();

        using var stateSubscription = store.States.Subscribe(observedStates.Add);

        await runtime.HandleAsync(emittedEffect, CancellationToken.None);

        await Assert.That(observedStates).IsEquivalentTo(new[]
        {
            new CounterState(0),
            new CounterState(5),
        });
    }

    [Test]
    public async Task HandleAsync_ignores_unhandled_effects()
    {
        var handledEffects = new List<CounterEffect>();
        var runtime = new MviEffectRuntime<CounterIntent>(
            new NullIntentSink<CounterIntent>(),
            new IEffectHandler<CounterEffect, CounterIntent>[]
            {
                new DelegateEffectHandler<CounterEffect, CounterIntent>((effect, _, _) =>
                {
                    handledEffects.Add(effect);
                    return ValueTask.CompletedTask;
                }),
            });

        await runtime.HandleAsync(new OtherEffect("ignored"), CancellationToken.None);

        await Assert.That(handledEffects).IsEmpty();
    }

    [Test]
    public async Task HandleAsync_propagates_handler_exceptions()
    {
        var failure = new InvalidOperationException("effect failed");
        var runtime = new MviEffectRuntime<CounterIntent>(
            new NullIntentSink<CounterIntent>(),
            new IEffectHandler<CounterEffect, CounterIntent>[]
            {
                new DelegateEffectHandler<CounterEffect, CounterIntent>((_, _, _) => throw failure),
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.HandleAsync(new CounterEffect("boom"), CancellationToken.None).AsTask());

        await Assert.That(exception).IsSameReferenceAs(failure);
    }

    private sealed record CounterState(int Count) : IMviState;

    private sealed record CounterIntent(int Kind, int Amount) : IMviIntent;

    private sealed record CounterEffect(string Message) : IMviEffect;

    private sealed record OtherEffect(string Message) : IMviEffect;

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

    private sealed class DelegateEffectHandler<TEffect, TIntent> : IEffectHandler<TEffect, TIntent>
        where TEffect : IMviEffect
        where TIntent : IMviIntent
    {
        private readonly Func<TEffect, IIntentSink<TIntent>, CancellationToken, ValueTask> handleAsync;

        public DelegateEffectHandler(
            Func<TEffect, IIntentSink<TIntent>, CancellationToken, ValueTask> handleAsync)
        {
            this.handleAsync = handleAsync;
        }

        public ValueTask HandleAsync(
            TEffect effect,
            IIntentSink<TIntent> sink,
            CancellationToken cancellationToken)
        {
            return handleAsync(effect, sink, cancellationToken);
        }
    }
}
