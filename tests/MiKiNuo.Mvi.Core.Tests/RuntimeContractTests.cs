using MiKiNuo.Mvi.Abstractions;
using MiKiNuo.Mvi.Core.Reducers;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class RuntimeContractTests
{
    [Test]
    public async Task StateOnly_preserves_state_and_has_no_effects()
    {
        var state = new CounterState(3);

        var result = ReduceResults.StateOnly<CounterState, CounterEffect>(state);

        await Assert.That(result.State).IsEqualTo(state);
        await Assert.That(result.Effects).IsEmpty();
    }

    [Test]
    public async Task WithEffect_preserves_state_and_single_effect()
    {
        var state = new CounterState(4);
        var effect = new CounterEffect("saved");

        var result = ReduceResults.WithEffect(state, effect);

        await Assert.That(result.State).IsEqualTo(state);
        await Assert.That(result.Effects).IsEquivalentTo(new[] { effect });
    }

    [Test]
    public async Task WithEffects_preserves_state_and_effect_order()
    {
        var state = new CounterState(5);
        var first = new CounterEffect("first");
        var second = new CounterEffect("second");

        var result = ReduceResults.WithEffects(state, new[] { first, second });

        await Assert.That(result.State).IsEqualTo(state);
        await Assert.That(result.Effects).IsEquivalentTo(new[] { first, second });
    }

    [Test]
    public async Task WithEffects_captures_effect_snapshot()
    {
        var state = new CounterState(6);
        var first = new CounterEffect("first");
        var effects = new List<CounterEffect> { first };

        var result = ReduceResults.WithEffects(state, effects);
        effects.Add(new CounterEffect("late"));

        await Assert.That(result.Effects).IsEquivalentTo(new[] { first });
    }

    private sealed record CounterState(int Count) : IMviState;

    private sealed record CounterIntent(int Kind) : IMviIntent;

    private sealed record CounterEffect(string Message) : IMviEffect;
}
