using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Runtime.MVI.Middleware;
using R3;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>取消、准入、停止与真正排空的契约。</summary>
public sealed class MviStoreLifetimeTests
{
    /// <summary>取消只取消操作；finally 仍能恢复已接纳的 Busy 状态。</summary>
    [Test] public async Task CallerCancellationAllowsOwnedCleanupMutationAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal();
        await using var store = TestCheck.Store(new(async (context, cancellationToken) =>
        {
            context.Reduce(new CounterMutation.Busy(true)); entered.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
            finally { context.TryReduce(static state => state.IsBusy, new CounterMutation.Busy(false)); }
        }));
        using CancellationTokenSource cancellation = new();
        Task operation = store.DispatchAsync(new CounterIntent.Run(), cancellation.Token).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancellation.Cancel();
        await TestCheck.ThrowsAsync<OperationCanceledException>(() => operation);
        await Assert.That(store.CurrentState.IsBusy).IsFalse();
    }
    /// <summary>停止后禁止任何提交，释放等待不响应取消的已准入操作真正退出。</summary>
    [Test] public async Task DisposeWaitsForInFlightHandlerAndRejectsLateCommitAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        bool accepted = true;
        var store = TestCheck.Store(new(async (context, _) =>
        { entered.TrySetResult(); await release.Task; accepted = context.TryReduce(static _ => true, new CounterMutation.Increment()); }));
        Task operation = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task closing = store.DisposeAsync().AsTask();
        bool early = closing.IsCompleted;
        release.TrySetResult(); await operation; await closing;
        await Assert.That(early).IsFalse(); await Assert.That(accepted).IsFalse();
        await TestCheck.ThrowsAsync<ObjectDisposedException>(() => store.DispatchAsync(new CounterIntent.Increment()).AsTask());
        await store.DisposeAsync();
    }
    /// <summary>限制包括外部等待在内的真实在途数量。</summary>
    [Test] public async Task ActiveCapacityRejectsExcessDispatchAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        await using var store = TestCheck.Store(new(async (_, cancellationToken) =>
        { entered.TrySetResult(); await release.Task.WaitAsync(cancellationToken); }), capacity: 1);
        Task operation = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try { await TestCheck.ThrowsAsync<MviCapacityExceededException>(() => store.DispatchAsync(new CounterIntent.Increment()).AsTask()); }
        finally { release.TrySetResult(); await operation; }
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }
    /// <summary>通知派发中的异常由 Errors 流观察。</summary>
    [Test] public async Task PostedFailureIsObservableAsync()
    {
        TaskCompletionSource<Exception> failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var store = TestCheck.Store(new((_, _) => throw new InvalidOperationException("probe")));
        using var subscription = store.Errors.Subscribe(error => failed.TrySetResult(error));
        await Assert.That(store.TryPost(new CounterIntent.Run())).IsTrue();
        Exception error = await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(error.Message).IsEqualTo("probe");
        store.Stop(); await Assert.That(store.TryPost(new CounterIntent.Increment())).IsFalse();
    }

    /// <summary>已接纳通知等待容量且不额外腾出隐藏队列空间。</summary>
    [Test]
    public async Task AcceptedNotificationsWaitForExecutionCapacityAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        TaskCompletionSource ready = TestCheck.Signal(), delivered = TestCheck.Signal();
        TaskCompletionSource<Exception> failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException warmupFailure = new("warmup");
        await using var store = new MviStore<CounterState, CounterIntent>(CounterState.Initial,
            new CounterHandler(async (_, _) =>
            {
                if (!ready.Task.IsCompleted) throw warmupFailure;
                entered.TrySetResult();
                await release.Task;
            }), new CounterReducer(), notificationCapacity: 2, maxConcurrentIntents: 1);
        using var states = store.States.Subscribe(state =>
        {
            if (state.Label == "delivered") delivered.TrySetResult();
        });
        using var errors = store.Errors.Subscribe(error =>
        {
            if (ReferenceEquals(error, warmupFailure)) ready.TrySetResult();
            else failed.TrySetResult(error);
        });
        store.TryPost(new CounterIntent.Run());
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task operation = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.That(store.TryPost(new CounterIntent.Increment())).IsTrue();
            await Assert.That(store.TryPost(new CounterIntent.Rename("delivered"))).IsTrue();
            await Task.Yield();
            await Assert.That(store.TryPost(new CounterIntent.Increment())).IsFalse();
            await Assert.That(store.CurrentState.Count).IsEqualTo(0);
        }
        finally { release.TrySetResult(); await operation.WaitAsync(TimeSpan.FromSeconds(5)); }
        await Task.WhenAny(delivered.Task, failed.Task).WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(failed.Task.IsCompleted).IsFalse();
        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }

    /// <summary>停止取消排队通知，但不能提前释放仍在运行的处理器。</summary>
    [Test]
    public async Task StopCancelsQueuedNotificationsButWaitsForRunningHandlerAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        var errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        await using var store = new MviStore<CounterState, CounterIntent>(CounterState.Initial,
            new CounterHandler(async (_, _) => { entered.TrySetResult(); await release.Task; }),
            new CounterReducer(), notificationCapacity: 1, maxConcurrentIntents: 1);
        using var subscription = store.Errors.Subscribe(errors.Enqueue);
        Task operation = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        Task closing = Task.CompletedTask;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.That(store.TryPost(new CounterIntent.Increment())).IsTrue();
            store.Stop();
            closing = store.DisposeAsync().AsTask();
            await Assert.That(closing.IsCompleted).IsFalse();
            await Assert.That(store.TryPost(new CounterIntent.Increment())).IsFalse();
            await TestCheck.ThrowsAsync<ObjectDisposedException>(() => store.DispatchAsync(new CounterIntent.Increment()).AsTask());
        }
        finally
        {
            release.TrySetResult();
            await operation.WaitAsync(TimeSpan.FromSeconds(5));
            await closing.WaitAsync(TimeSpan.FromSeconds(5));
        }
        await Assert.That(errors.IsEmpty).IsTrue();
        await Assert.That(store.CurrentState.Count).IsEqualTo(0);
    }

    /// <summary>后台失败仍释放执行容量。</summary>
    [Test]
    public async Task PostedFailureReleasesExecutionCapacityAsync()
    {
        TaskCompletionSource<Exception> failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var store = TestCheck.Store(new((_, _) => throw new InvalidOperationException("posted")), capacity: 1);
        using var subscription = store.Errors.Subscribe(error => failed.TrySetResult(error));
        await Assert.That(store.TryPost(new CounterIntent.Run())).IsTrue();
        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }
}
