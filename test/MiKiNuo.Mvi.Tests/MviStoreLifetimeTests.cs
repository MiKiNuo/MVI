using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Composition;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证存储关闭时等待在途副作用安全退出。</summary>
public sealed class MviStoreLifetimeTests
{
    /// <summary>子级慢操作未退出时，父级也须停止准入并取消在途操作。</summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000", Justification = "存储与子实例所有权转交父实例，由 finally 等待整树释放。")]
    public async Task ClosingTreeStopsParentBeforeChildDrainsAsync()
    {
        WaitingMiddleware middleware = new();
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> parentStore = new(
            StoreTestState.Initial, new StoreTestReducer(), new SlowEffectDispatcher(), [middleware]);
        SlowEffectDispatcher childDispatcher = new();
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> childStore = new(
            StoreTestState.Initial, new StoreTestReducer(), childDispatcher);
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", [parentStore]);
        MviFeatureInstance<string> child = new(Guid.NewGuid(), "子", [childStore]);
        await parent.Own(child);
        Task parentDispatch = parentStore.DispatchAsync(new StoreTestIntent.Increment()).AsTask();
        await middleware.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task childDispatch = childStore.DispatchAsync(new StoreTestIntent.TriggerSlow()).AsTask();
        await childDispatcher.SlowEffectStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        TaskCompletionSource stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = parent.Lifetime.Register(() => stopping.TrySetResult());
        Task closing = parent.DisposeAsync().AsTask();
        try
        {
            await stopping.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.That(parentStore.TryPost(new StoreTestIntent.Increment())).IsFalse();
            await Assert.That(middleware.Token.IsCancellationRequested).IsTrue();
            await Assert.That(closing.IsCompleted).IsFalse();
            await Assert.That(async () => await parentStore.DispatchAsync(new StoreTestIntent.Increment()))
                .Throws<ObjectDisposedException>();
        }
        finally
        {
            middleware.Release.TrySetResult();
            childDispatcher.ReleaseSlowEffect();
            try { await parentDispatch; } catch (ObjectDisposedException) { }
            await childDispatch;
            await closing;
        }
    }

    /// <summary>通知队列满时拒绝接纳，不静默覆盖已接纳意图。</summary>
    [Test]
    public async Task TryPost_RejectsOverflowAsync()
    {
        WaitingMiddleware middleware = new();
        await using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial, new StoreTestReducer(), new SlowEffectDispatcher(), [middleware], 1);
        await Assert.That(store.TryPost(new StoreTestIntent.Increment())).IsTrue();
        await middleware.Started.Task;
        try
        {
            await Assert.That(store.TryPost(new StoreTestIntent.Increment())).IsTrue();
            await Assert.That(store.TryPost(new StoreTestIntent.Increment())).IsFalse();
        }
        finally
        {
            middleware.Release.TrySetResult();
        }
        TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = store.States.Subscribe(state =>
        {
            if (state.Count == 2)
            {
                changed.TrySetResult();
            }
        });
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>通知副作用故障可观察，故障之后仍能处理新意图。</summary>
    [Test]
    public async Task TryPost_ReportsEffectFailureAsync()
    {
        await using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial, new StoreTestReducer(), new FailingDispatcher());
        TaskCompletionSource<Exception> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = store.Errors.Subscribe(exception => error.TrySetResult(exception));
        await Assert.That(store.TryPost(new StoreTestIntent.TriggerEffect())).IsTrue();
        Exception observed = await error.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(observed).IsTypeOf<InvalidOperationException>();
        await store.DispatchAsync(new StoreTestIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }

    /// <summary>关闭同步发出取消，忽略取消的中间件完成后不得发布迟到状态。</summary>
    [Test]
    public async Task Dispose_CancelsAndRejectsLateStateAsync()
    {
        WaitingMiddleware middleware = new();
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial, new StoreTestReducer(), new SlowEffectDispatcher(), [middleware]);
        int lastCount = -1;
        using IDisposable subscription = store.States.Subscribe(state => lastCount = state.Count);
        Task dispatch = store.DispatchAsync(new StoreTestIntent.Increment()).AsTask();
        await middleware.Started.Task;
        store.Dispose();
        bool cancellationRequested = middleware.Token.IsCancellationRequested;
        Task closing = store.DisposeAsync().AsTask();
        middleware.Release.TrySetResult();
        await Assert.That(async () => await dispatch).Throws<ObjectDisposedException>();
        await closing;
        await Assert.That(cancellationRequested).IsTrue();
        await Assert.That(lastCount).IsEqualTo(0);
    }

    /// <summary>通知规约不等待前一通知的慢副作用，关闭后拒绝接纳。</summary>
    [Test]
    public async Task TryPost_ContinuesWhileEffectIsWaitingAsync()
    {
        SlowEffectDispatcher dispatcher = new();
        await using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial, new StoreTestReducer(), dispatcher, notificationCapacity: 1);
        TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = store.States.Subscribe(state =>
        {
            if (state.Count == 1)
            {
                changed.TrySetResult();
            }
        });
        await Assert.That(store.TryPost(new StoreTestIntent.TriggerSlow())).IsTrue();
        await dispatcher.SlowEffectStarted.Task;
        try
        {
            await Assert.That(store.TryPost(new StoreTestIntent.Increment())).IsTrue();
            await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            dispatcher.ReleaseSlowEffect();
        }
        store.Dispose();
        await Assert.That(store.TryPost(new StoreTestIntent.Increment())).IsFalse();
    }

    /// <summary>关闭立即拒绝新派发，异步释放等待忽略取消的副作用。</summary>
    [Test]
    public async Task DisposeAsync_WaitsForAdmittedEffectAsync()
    {
        SlowEffectDispatcher dispatcher = new();
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial, new StoreTestReducer(), dispatcher);
        Task dispatch = store.DispatchAsync(new StoreTestIntent.TriggerSlow()).AsTask();
        await dispatcher.SlowEffectStarted.Task;
        store.Dispose();
        Task closing = store.DisposeAsync().AsTask();
        await Assert.That(closing.IsCompleted).IsFalse();
        await Assert.That(async () => await store.DispatchAsync(new StoreTestIntent.Increment()))
            .Throws<ObjectDisposedException>();
        dispatcher.ReleaseSlowEffect();
        await dispatch;
        await closing;
    }

    /// <summary>模拟等待外部输入且忽略取消的中间件。</summary>
    private sealed class WaitingMiddleware : IMviMiddleware<StoreTestState, StoreTestIntent, StoreTestEffect>
    {
        /// <summary>获取进入等待的信号。</summary>
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>获取外部释放信号。</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>获取实际收到的取消标记。</summary>
        public CancellationToken Token { get; private set; }
        /// <summary>等待外部释放后继续规约。</summary>
        /// <param name="context">当前意图上下文。</param>
        /// <param name="nextMiddleware">后续管线。</param>
        /// <param name="cancellationToken">所属操作取消标记。</param>
        /// <returns>规约结果。</returns>
        public async ValueTask<MviReduceResult<StoreTestState, StoreTestEffect>> InvokeAsync(
            MviMiddlewareContext<StoreTestState, StoreTestIntent, StoreTestEffect> context,
            MviMiddlewareStep<StoreTestState, StoreTestIntent, StoreTestEffect> nextMiddleware,
            CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Started.TrySetResult();
            await Release.Task;
            return await nextMiddleware(context, cancellationToken);
        }
    }

    /// <summary>模拟外部副作用失败的分发器。</summary>
    private sealed class FailingDispatcher : IMviEffectDispatcher<StoreTestEffect>
    {
        /// <summary>报告外部操作失败。</summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>失败的派发任务。</returns>
        public ValueTask DispatchAsync(StoreTestEffect effect, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("外部操作失败");
        }
    }
}
