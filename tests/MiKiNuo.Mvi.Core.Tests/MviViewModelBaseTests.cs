using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Dispatching;
using MiKiNuo.Mvi.Core.Reducers;
using MiKiNuo.Mvi.Core.Store;
using MiKiNuo.Mvi.Core.ViewModels;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviViewModelBaseTests
{
    [Test]
    public async Task ViewModelBase_applies_initial_and_reduced_store_state()
    {
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) => ValueTask.FromResult(
                ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + intent.Amount))));
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(new CounterState(1), dispatcher);
        var viewModel = new TestViewModel(store);

        await viewModel.DispatchForTestAsync(new CounterIntent(1, 4));

        await Assert.That(viewModel.AppliedStates).IsEquivalentTo(new[]
        {
            new CounterState(1),
            new CounterState(5),
        });
    }

    [Test]
    public async Task ViewModelBase_dispatches_intents_through_store()
    {
        var dispatchedIntents = new List<CounterIntent>();
        var dispatcher = new DelegateIntentDispatcher<CounterState, CounterIntent, CounterEffect>(
            (state, intent, _) =>
            {
                dispatchedIntents.Add(intent);
                return ValueTask.FromResult(
                    ReduceResults.StateOnly<CounterState, CounterEffect>(new CounterState(state.Count + intent.Amount)));
            });
        var store = new MviStore<CounterState, CounterIntent, CounterEffect>(new CounterState(0), dispatcher);
        var viewModel = new TestViewModel(store);
        var intent = new CounterIntent(2, 3);

        await viewModel.DispatchForTestAsync(intent);

        await Assert.That(dispatchedIntents).IsEquivalentTo(new[] { intent });
    }

    private sealed class TestViewModel : MviViewModelBase<CounterState, CounterIntent, CounterEffect>
    {
        public TestViewModel(IMviStore<CounterState, CounterIntent, CounterEffect> store)
            : base(store)
        {
        }

        public List<CounterState> AppliedStates { get; } = new();

        public ValueTask DispatchForTestAsync(CounterIntent intent)
        {
            return DispatchAsync(intent);
        }

        protected override void ApplyStateCore(CounterState state)
        {
            AppliedStates.Add(state);
        }
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
}
