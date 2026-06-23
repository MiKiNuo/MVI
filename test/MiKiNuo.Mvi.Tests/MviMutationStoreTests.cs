using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示变更存储测试。
/// </summary>
public sealed class MviMutationStoreTests
{
    /// <summary>
    /// 验证派发意图后状态通过变更更新。
    /// </summary>
    [Test]
    public async Task Dispatch_Should_UpdateStateAsync()
    {
        CounterState initialState = new(0);
        using MviMutationStore<CounterState, CounterIntent, CounterMutation, CounterEffect> store = new(
            initialState,
            new CounterIntentHandler(),
            new CounterMutationReducer(),
            new CounterEffectDispatcher());

        await store.DispatchAsync(new CounterIntent.Increment(), CancellationToken.None);

        await Assert.That(store.CurrentState.Value).IsEqualTo(1);
    }

    /// <summary>
    /// 验证派发意图后动作副作用被分发。
    /// </summary>
    [Test]
    public async Task Dispatch_Should_DispatchActionEffectsAsync()
    {
        CounterState initialState = new(0);
        CounterEffectDispatcher effectDispatcher = new();
        using MviMutationStore<CounterState, CounterIntent, CounterMutation, CounterEffect> store = new(
            initialState,
            new CounterIntentHandler(),
            new CounterMutationReducer(),
            effectDispatcher);

        await store.DispatchAsync(new CounterIntent.EmitEffect(), CancellationToken.None);

        await Assert.That(effectDispatcher.DispatchedEffects.Count).IsEqualTo(1);
        await Assert.That(effectDispatcher.DispatchedEffects[0]).IsTypeOf<CounterEffect.Log>();
    }

    /// <summary>
    /// 验证派发意图后派生副作用从规约器产生。
    /// </summary>
    [Test]
    public async Task Dispatch_Should_DispatchDerivedEffectsAsync()
    {
        CounterState initialState = new(0);
        CounterEffectDispatcher effectDispatcher = new();
        using MviMutationStore<CounterState, CounterIntent, CounterMutation, CounterEffect> store = new(
            initialState,
            new CounterIntentHandler(),
            new CounterMutationReducer(),
            effectDispatcher);

        await store.DispatchAsync(new CounterIntent.TriggerDerivedEffect(), CancellationToken.None);

        await Assert.That(effectDispatcher.DispatchedEffects.Count).IsEqualTo(1);
        await Assert.That(effectDispatcher.DispatchedEffects[0]).IsTypeOf<CounterEffect.Derived>();
    }

    /// <summary>
    /// 验证状态变化流推送新状态。
    /// </summary>
    [Test]
    public async Task States_Should_EmitNewStateAsync()
    {
        CounterState initialState = new(0);
        using MviMutationStore<CounterState, CounterIntent, CounterMutation, CounterEffect> store = new(
            initialState,
            new CounterIntentHandler(),
            new CounterMutationReducer(),
            new CounterEffectDispatcher());

        System.Collections.Generic.List<CounterState> emitted = [];
        System.IDisposable subscription = store.States.Subscribe(x => emitted.Add(x));

        await store.DispatchAsync(new CounterIntent.Increment(), CancellationToken.None);

        subscription.Dispose();
        await Assert.That(emitted.Count).IsEqualTo(2);
        await Assert.That(emitted[1].Value).IsEqualTo(1);
    }
}

/// <summary>
/// 表示计数状态。
/// </summary>
/// <param name="Value">计数值。</param>
public sealed record CounterState(int Value) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static CounterState Initial { get; } = new(0);
}

/// <summary>
/// 表示计数意图。
/// </summary>
public abstract partial record CounterIntent : IMviIntent
{
    /// <summary>表示递增意图。</summary>
    public sealed partial record Increment : CounterIntent;

    /// <summary>表示触发动作副作用意图。</summary>
    public sealed partial record EmitEffect : CounterIntent;

    /// <summary>表示触发派生副作用意图。</summary>
    public sealed partial record TriggerDerivedEffect : CounterIntent;
}

/// <summary>
/// 表示计数变更。
/// </summary>
public abstract partial record CounterMutation : IMviMutation<CounterState>
{
    /// <summary>
    /// 表示增加计数值。
    /// </summary>
    public sealed partial record AddValue : CounterMutation
    {
        /// <summary>
        /// 初始化增加计数值变更。
        /// </summary>
        /// <param name="amount">增加量。</param>
        public AddValue(int amount)
        {
            Amount = amount;
        }

        /// <summary>
        /// 获取增加量。
        /// </summary>
        public int Amount { get; init; }
    }

    /// <summary>表示触发派生副作用。</summary>
    public sealed partial record TriggerDerived : CounterMutation;
}

/// <summary>
/// 表示计数副作用。
/// </summary>
public abstract partial record CounterEffect : IMviEffect
{
    /// <summary>
    /// 表示日志副作用。
    /// </summary>
    public sealed partial record Log : CounterEffect
    {
        /// <summary>
        /// 初始化日志副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Log(string text)
        {
            Text = text;
        }

        /// <summary>
        /// 获取日志文本。
        /// </summary>
        public string Text { get; init; }
    }

    /// <summary>表示派生副作用。</summary>
    public sealed partial record Derived : CounterEffect;
}

/// <summary>
/// 表示计数意图处理器。
/// </summary>
public sealed class CounterIntentHandler
    : IMviIntentHandler<CounterState, CounterIntent, CounterMutation, CounterEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    public ValueTask<MviHandleResult<CounterMutation, CounterEffect>> HandleAsync(
        CounterState state,
        CounterIntent intent,
        CancellationToken cancellationToken = default)
    {
        MviHandleResult<CounterMutation, CounterEffect> result = intent switch
        {
            CounterIntent.Increment => MviHandleResult
                .Mutations<CounterMutation, CounterEffect>(new CounterMutation.AddValue(1)),
            CounterIntent.EmitEffect => MviHandleResult
                .MutationsAndEffects<CounterMutation, CounterEffect>(
                    Array.Empty<CounterMutation>(),
                    new CounterEffect[] { new CounterEffect.Log("动作副作用") }),
            CounterIntent.TriggerDerivedEffect => MviHandleResult
                .Mutations<CounterMutation, CounterEffect>(new CounterMutation.TriggerDerived()),
            _ => MviHandleResult.Empty<CounterMutation, CounterEffect>(),
        };
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// 表示计数变更规约器。
/// </summary>
public sealed class CounterMutationReducer
    : MviMutationReducerBase<CounterState, CounterMutation, CounterEffect>
{
    /// <summary>
    /// 将变更应用到状态。
    /// </summary>
    public override MviReduceResult<CounterState, CounterEffect> Reduce(
        CounterState state,
        CounterMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);

        return mutation switch
        {
            CounterMutation.AddValue add => MviReduceResult
                .State<CounterState, CounterEffect>(state with { Value = state.Value + add.Amount }),
            CounterMutation.TriggerDerived => MviReduceResult
                .StateAndEffect<CounterState, CounterEffect>(state, new CounterEffect.Derived()),
            _ => MviReduceResult.State<CounterState, CounterEffect>(state),
        };
    }
}

/// <summary>
/// 表示计数副作用分发器。
/// </summary>
public sealed class CounterEffectDispatcher : IMviEffectDispatcher<CounterEffect>
{
    /// <summary>
    /// 获取已分发的副作用列表。
    /// </summary>
    public System.Collections.Generic.List<CounterEffect> DispatchedEffects { get; } = [];

    /// <summary>
    /// 分发副作用。
    /// </summary>
    public ValueTask DispatchAsync(CounterEffect effect, CancellationToken cancellationToken = default)
    {
        DispatchedEffects.Add(effect);
        return ValueTask.CompletedTask;
    }
}
