using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Application.MVI.Middleware;
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
    /// 验证请求意图与后续意图各规约一次，且完整副作用序列对中间件和分发器均可见。
    /// </summary>
    [Test]
    public async Task DispatchAsync_Should_ReduceEachIntentOnceAndExposeAllEffectsAsync()
    {
        ChainedReducer reducer = new();
        ChainedEffectDispatcher dispatcher = new();
        RecordingDiagnosticSink sink = new();
        MviLoggingMiddleware<ChainedState, ChainedIntent, ChainedEffect> logging = new(sink, "Chained");
        using MviStore<ChainedState, ChainedIntent, ChainedEffect> store = new(
            new ChainedState(0),
            new ChainedIntentHandler(),
            reducer,
            dispatcher,
            new[] { logging });

        await store.DispatchAsync(new ChainedIntent.Start());

        await Assert.That(store.CurrentState.Value).IsEqualTo(11);
        await Assert.That(reducer.Reduced.Count).IsEqualTo(2);
        await Assert.That(reducer.Reduced[0]).IsTypeOf<ChainedIntent.Start>();
        await Assert.That(reducer.Reduced[1]).IsTypeOf<ChainedIntent.Completed>();
        await Assert.That(dispatcher.Dispatched.Select(static effect => effect.Name))
            .IsEquivalentTo(new[] { "start", "completed" });
        await Assert.That(sink.Entries.Any(static entry =>
            entry.Stage == "Reducer" && entry.Message.Contains("2 个 Effect", StringComparison.Ordinal))).IsTrue();
    }

    /// <summary>
    /// 表示后续意图测试状态。
    /// </summary>
    /// <param name="Value">状态值。</param>
    private sealed record ChainedState(int Value) : MiKiNuo.Mvi.Domain.MVI.State.IMviState;

    /// <summary>
    /// 表示后续意图测试意图。
    /// </summary>
    private abstract record ChainedIntent : MiKiNuo.Mvi.Domain.MVI.Intent.IMviIntent
    {
        /// <summary>表示初始意图。</summary>
        public sealed record Start : ChainedIntent;

        /// <summary>表示业务完成后的后续意图。</summary>
        public sealed record Completed : ChainedIntent;
    }

    /// <summary>
    /// 表示带名称的测试副作用。
    /// </summary>
    /// <param name="Name">副作用名称。</param>
    private sealed record ChainedEffect(string Name) : MiKiNuo.Mvi.Domain.MVI.Effect.IMviEffect;

    /// <summary>
    /// 表示记录规约次数的测试规约器。
    /// </summary>
    private sealed class ChainedReducer
        : MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<ChainedState, ChainedIntent, ChainedEffect>
    {
        /// <summary>获取已规约的意图。</summary>
        public List<ChainedIntent> Reduced { get; } = [];

        /// <summary>规约意图。</summary>
        /// <param name="state">当前状态。</param>
        /// <param name="intent">意图。</param>
        /// <returns>规约结果。</returns>
        public MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<ChainedState, ChainedEffect> Reduce(
            ChainedState state,
            ChainedIntent intent)
        {
            Reduced.Add(intent);
            return intent switch
            {
                ChainedIntent.Start => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.StateAndEffect(
                    state with { Value = state.Value + 1 },
                    new ChainedEffect("start")),
                ChainedIntent.Completed => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.StateAndEffect(
                    state with { Value = state.Value + 10 },
                    new ChainedEffect("completed")),
                _ => MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult.State<ChainedState, ChainedEffect>(state),
            };
        }
    }

    /// <summary>
    /// 表示返回后续意图的测试处理器。
    /// </summary>
    private sealed class ChainedIntentHandler
        : MiKiNuo.Mvi.Application.MVI.IntentHandler.IMviIntentHandler<ChainedState, ChainedIntent>
    {
        /// <summary>处理初始意图。</summary>
        /// <param name="state">当前状态。</param>
        /// <param name="intent">意图。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>后续意图。</returns>
        public ValueTask<ChainedIntent?> HandleAsync(
            ChainedState state,
            ChainedIntent intent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ChainedIntent? followUp = intent is ChainedIntent.Start ? new ChainedIntent.Completed() : null;
            return ValueTask.FromResult(followUp);
        }
    }

    /// <summary>
    /// 表示记录副作用的测试分发器。
    /// </summary>
    private sealed class ChainedEffectDispatcher
        : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<ChainedEffect>
    {
        /// <summary>获取已分发的副作用。</summary>
        public List<ChainedEffect> Dispatched { get; } = [];

        /// <summary>记录副作用。</summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>已完成的任务。</returns>
        public ValueTask DispatchAsync(ChainedEffect effect, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dispatched.Add(effect);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 表示记录诊断条目的测试接收器。
    /// </summary>
    private sealed class RecordingDiagnosticSink : IMviDiagnosticSink
    {
        /// <summary>获取诊断条目。</summary>
        public List<MviDiagnosticEntry> Entries { get; } = [];

        /// <summary>记录诊断条目。</summary>
        /// <param name="entry">诊断条目。</param>
        public void Record(MviDiagnosticEntry entry) => Entries.Add(entry);
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
