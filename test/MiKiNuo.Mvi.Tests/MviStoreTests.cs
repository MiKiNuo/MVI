using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MviStore 核心派发语义测试：单决策点、锁只护 Reduce、副作用锁外派发、回流意图重进管线。
/// </summary>
public sealed class MviStoreTests
{
    /// <summary>
    /// 验证派发意图后状态被规约并发布。
    /// </summary>
    [Test]
    public async Task Dispatch_Should_ReduceAndPublishStateAsync()
    {
        using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = CreateStore();

        await store.DispatchAsync(new StoreTestIntent.Increment());
        await store.DispatchAsync(new StoreTestIntent.Increment());

        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }

    /// <summary>
    /// 验证副作用在状态发布之后执行：EffectDispatcher 看到的 CurrentState 已是新状态。
    /// </summary>
    [Test]
    public async Task Dispatch_Should_PublishStateBeforeDispatchingEffectsAsync()
    {
        RecordingEffectDispatcher dispatcher = new();
        using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = CreateStore(dispatcher);
        dispatcher.Store = store;

        await store.DispatchAsync(new StoreTestIntent.TriggerEffect());

        await Assert.That(dispatcher.EffectsSeen).IsEquivalentTo(new[] { nameof(StoreTestEffect.Ping) });
        await Assert.That(dispatcher.StateCountAtDispatch).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 EffectDispatcher 回流的新意图作为普通派发重新进入管线，中间件全程可见。
    /// </summary>
    [Test]
    public async Task FeedbackIntent_Should_ReenterThroughMiddlewareAsync()
    {
        RecordingMiddleware middleware = new();
        FeedbackEffectDispatcher dispatcher = new();
        using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store =
            CreateStore(dispatcher, [middleware]);

        await store.DispatchAsync(new StoreTestIntent.TriggerFeedback());

        await Assert.That(middleware.SeenIntents).IsEquivalentTo(
            new[] { nameof(StoreTestIntent.TriggerFeedback), nameof(StoreTestIntent.Increment) });
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }

    /// <summary>
    /// 验证慢副作用不阻塞同 Store 的其他意图：副作用在锁外执行。
    /// </summary>
    [Test]
    public async Task SlowEffect_Should_NotBlockOtherIntentsAsync()
    {
        SlowEffectDispatcher dispatcher = new();
        using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = CreateStore(dispatcher);

        ValueTask slowDispatch = store.DispatchAsync(new StoreTestIntent.TriggerSlow());
        await dispatcher.SlowEffectStarted.Task;

        await store.DispatchAsync(new StoreTestIntent.Increment());

        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
        await Assert.That(slowDispatch.IsCompleted).IsFalse();

        dispatcher.ReleaseSlowEffect();
        await slowDispatch;
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }

    /// <summary>
    /// 验证释放后派发抛出 ObjectDisposedException。
    /// </summary>
    [Test]
    public async Task Dispatch_AfterDispose_Should_ThrowAsync()
    {
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = CreateStore();
        store.Dispose();

        await Assert.That(async () => await store.DispatchAsync(new StoreTestIntent.Increment()))
            .Throws<ObjectDisposedException>();
    }

    private static MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> CreateStore(
        IMviEffectDispatcher<StoreTestEffect>? effectDispatcher = null,
        IReadOnlyList<IMviMiddleware<StoreTestState, StoreTestIntent, StoreTestEffect>>? middlewares = null)
    {
        return new MviStore<StoreTestState, StoreTestIntent, StoreTestEffect>(
            StoreTestState.Initial,
            new StoreTestReducer(),
            effectDispatcher ?? new NoopEffectDispatcher<StoreTestEffect>(),
            middlewares);
    }
}

/// <summary>
/// 表示 Store 测试状态。
/// </summary>
/// <param name="Count">计数。</param>
public sealed record StoreTestState(int Count) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static StoreTestState Initial { get; } = new(0);
}

/// <summary>
/// 表示 Store 测试意图。
/// </summary>
public abstract record StoreTestIntent : IMviIntent
{
    /// <summary>
    /// 表示计数加一意图。
    /// </summary>
    public sealed record Increment : StoreTestIntent;

    /// <summary>
    /// 表示触发探测副作用意图。
    /// </summary>
    public sealed record TriggerEffect : StoreTestIntent;

    /// <summary>
    /// 表示触发回流副作用意图。
    /// </summary>
    public sealed record TriggerFeedback : StoreTestIntent;

    /// <summary>
    /// 表示触发慢副作用意图。
    /// </summary>
    public sealed record TriggerSlow : StoreTestIntent;
}

/// <summary>
/// 表示 Store 测试副作用。
/// </summary>
public abstract record StoreTestEffect : IMviEffect
{
    /// <summary>
    /// 表示探测副作用。
    /// </summary>
    public sealed record Ping : StoreTestEffect;

    /// <summary>
    /// 表示回流副作用：执行后回流 Increment 意图。
    /// </summary>
    public sealed record FollowUp : StoreTestEffect;

    /// <summary>
    /// 表示慢副作用。
    /// </summary>
    public sealed record Slow : StoreTestEffect;
}

/// <summary>
/// 表示 Store 测试规约器（直接实现接口，不走源生成器）。
/// </summary>
public sealed class StoreTestReducer : IMviReducer<StoreTestState, StoreTestIntent, StoreTestEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public MviReduceResult<StoreTestState, StoreTestEffect> Reduce(
        StoreTestState state,
        StoreTestIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            StoreTestIntent.Increment => MviReduceResult.State<StoreTestState, StoreTestEffect>(
                state with { Count = state.Count + 1 }),
            StoreTestIntent.TriggerEffect => MviReduceResult.StateAndEffects<StoreTestState, StoreTestEffect>(
                state with { Count = state.Count + 1 },
                new StoreTestEffect[] { new StoreTestEffect.Ping() }),
            StoreTestIntent.TriggerFeedback => MviReduceResult.StateAndEffect<StoreTestState, StoreTestEffect>(
                state,
                new StoreTestEffect.FollowUp()),
            StoreTestIntent.TriggerSlow => MviReduceResult.StateAndEffect<StoreTestState, StoreTestEffect>(
                state,
                new StoreTestEffect.Slow()),
            _ => MviReduceResult.State<StoreTestState, StoreTestEffect>(state),
        };
    }
}

/// <summary>
/// 表示记录副作用派发时状态的测试分发器。
/// </summary>
public sealed class RecordingEffectDispatcher : IMviEffectDispatcher<StoreTestEffect>
{
    private readonly List<string> _effectsSeen = [];

    /// <summary>
    /// 获取或设置状态观察目标 Store（由测试在 Store 构造后注入）。
    /// </summary>
    public IMviStore<StoreTestState, StoreTestIntent, StoreTestEffect>? Store { get; set; }

    /// <summary>
    /// 获取已派发的副作用名称序列。
    /// </summary>
    public IReadOnlyList<string> EffectsSeen => _effectsSeen;

    /// <summary>
    /// 获取派发副作用瞬间观察到的状态计数。
    /// </summary>
    public int StateCountAtDispatch { get; private set; } = -1;

    /// <summary>
    /// 记录副作用并由外部设置状态观察值。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已完成的任务。</returns>
    public ValueTask DispatchAsync(StoreTestEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effectsSeen.Add(effect.GetType().Name);
        StateCountAtDispatch = Store?.CurrentState.Count ?? -1;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 表示回流测试分发器：Ping 记录后由 FollowUp 回流 Increment 意图。
/// </summary>
public sealed partial class FeedbackEffectDispatcher
    : MviEffectDispatcherBase<StoreTestIntent, StoreTestEffect>
{
    /// <summary>
    /// 处理回流副作用：回流 Increment 意图。
    /// </summary>
    [MviEffect(typeof(StoreTestEffect.FollowUp))]
    private async ValueTask HandleFollowUp(
        StoreTestEffect.FollowUp effect,
        CancellationToken cancellationToken)
    {
        await DispatchIntentAsync(new StoreTestIntent.Increment(), cancellationToken);
    }
}

/// <summary>
/// 表示慢副作用测试分发器：Slow 副作用阻塞直至显式释放。
/// </summary>
public sealed class SlowEffectDispatcher : IMviEffectDispatcher<StoreTestEffect>
{
    private readonly TaskCompletionSource _slowRelease = new();

    /// <summary>
    /// 获取慢副作用已开始执行的通知。
    /// </summary>
    public TaskCompletionSource SlowEffectStarted { get; } = new();

    /// <summary>
    /// 释放慢副作用。
    /// </summary>
    public void ReleaseSlowEffect() => _slowRelease.TrySetResult();

    /// <summary>
    /// 派发副作用：Slow 阻塞直至释放。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    public async ValueTask DispatchAsync(StoreTestEffect effect, CancellationToken cancellationToken = default)
    {
        if (effect is StoreTestEffect.Slow)
        {
            SlowEffectStarted.TrySetResult();
            await _slowRelease.Task;
        }
    }
}

/// <summary>
/// 表示记录途经意图的测试中间件。
/// </summary>
public sealed class RecordingMiddleware : IMviMiddleware<StoreTestState, StoreTestIntent, StoreTestEffect>
{
    private readonly List<string> _seenIntents = [];

    /// <summary>
    /// 获取已记录的意图名称序列。
    /// </summary>
    public IReadOnlyList<string> SeenIntents => _seenIntents;

    /// <summary>
    /// 记录意图并继续管线。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一个中间件。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>规约结果。</returns>
    public async ValueTask<MviReduceResult<StoreTestState, StoreTestEffect>> InvokeAsync(
        MviMiddlewareContext<StoreTestState, StoreTestIntent, StoreTestEffect> context,
        MviMiddlewareStep<StoreTestState, StoreTestIntent, StoreTestEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);
        _seenIntents.Add(context.Intent.GetType().Name);
        return await nextMiddleware(context, cancellationToken);
    }
}
