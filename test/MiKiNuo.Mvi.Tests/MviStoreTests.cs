using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI Store 测试。
/// </summary>
public sealed class MviStoreTests
{
    /// <summary>
    /// 验证 Store 可以通过 Intent 更新状态。
    /// </summary>
    [Test]
    public async Task DispatchAsync_Should_UpdateCurrentStateAsync()
    {
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new NoopEffectDispatcher<LoginEffect>());

        await store.DispatchAsync(new LoginIntent.ChangeUserName("admin"));

        await Assert.That(store.CurrentState.UserName).IsEqualTo("admin");
    }

    /// <summary>
    /// 验证 EffectDispatcher 向同一 Store 再派发 Intent 不会死锁（重入安全）。
    /// </summary>
    [Test]
    public async Task DispatchAsync_Should_BeReentrantSafe_WhenEffectRedispatchesAsync()
    {
        ReentryDispatcher dispatcher = new();
        using MviStore<ReentryState, ReentryIntent, LoopEffect> store = new(
            new ReentryState(0),
            new ReentryIntentHandler(),
            new ReentryReducer(),
            dispatcher);
        dispatcher.OnEffect = async () =>
        {
            if (store.CurrentState.Count < 2)
            {
                await store.DispatchAsync(new ReentryIntent.Fire());
            }
        };

        Task dispatchTask = store.DispatchAsync(new ReentryIntent.Fire()).AsTask();
        Task completed = await Task.WhenAny(dispatchTask, Task.Delay(TimeSpan.FromSeconds(10)));

        await Assert.That(ReferenceEquals(completed, dispatchTask)).IsTrue();
        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }

    /// <summary>
    /// 表示重入测试用状态。
    /// </summary>
    /// <param name="Count">计数值。</param>
    private sealed record ReentryState(int Count) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

    /// <summary>
    /// 表示重入测试用意图。
    /// </summary>
    private abstract record ReentryIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
    {
        /// <summary>
        /// 表示触发副作用的意图。
        /// </summary>
        public sealed record Fire : ReentryIntent;
    }

    /// <summary>
    /// 表示重入测试用回环副作用。
    /// </summary>
    private sealed record LoopEffect : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

    /// <summary>
    /// 表示重入测试用规约器。
    /// </summary>
    private sealed class ReentryReducer
        : MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<ReentryState, ReentryIntent, LoopEffect>
    {
        /// <summary>
        /// 将意图规约为新状态与回环副作用。
        /// </summary>
        /// <param name="state">当前状态。</param>
        /// <param name="intent">用户意图。</param>
        /// <returns>规约结果。</returns>
        public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<ReentryState, LoopEffect> Reduce(
            ReentryState state,
            ReentryIntent intent)
        {
            return intent is ReentryIntent.Fire
                ? MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.StateAndEffect(
                    state with { Count = state.Count + 1 },
                    new LoopEffect())
                : MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<ReentryState, LoopEffect>(state);
        }
    }

    /// <summary>
    /// 表示重入测试用空意图处理器。
    /// </summary>
    private sealed class ReentryIntentHandler
        : MiKiNuo.Mvi.Application.MVI.IntentHandler.IMviIntentHandler<ReentryState, ReentryIntent>
    {
        /// <summary>
        /// 处理意图（空操作，返回无后续意图）。
        /// </summary>
        /// <param name="state">当前状态。</param>
        /// <param name="intent">用户意图。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>无后续意图。</returns>
        public async ValueTask<ReentryIntent?> HandleAsync(
            ReentryState state,
            ReentryIntent intent,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return null;
        }
    }

    /// <summary>
    /// 表示重入测试用副作用分发器，收到副作用后回调再派发。
    /// </summary>
    private sealed class ReentryDispatcher
        : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<LoopEffect>
    {
        /// <summary>
        /// 获取或设置副作用回调。
        /// </summary>
        public Func<ValueTask>? OnEffect { get; set; }

        /// <summary>
        /// 分发副作用并触发回调。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步派发过程的任务。</returns>
        public async ValueTask DispatchAsync(LoopEffect effect, CancellationToken cancellationToken = default)
        {
            if (OnEffect is not null)
            {
                await OnEffect();
            }
        }
    }
}
