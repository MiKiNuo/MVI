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
/// <summary>唯一的 Handler/Mutation Store 行为回归。</summary>
public sealed class MviStoreTests
{
    /// <summary>普通派发提交状态并保留按序状态流。</summary>
    [Test] public async Task DispatchPublishesCommittedStatesAsync()
    {
        await using var store = TestCheck.Store();
        List<int> observed = [];
        using var subscription = store.States.Subscribe(state => observed.Add(state.Count));
        await store.DispatchAsync(new CounterIntent.Increment());
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(string.Join(",", observed)).IsEqualTo("0,1,2");
        await Assert.That(store.CurrentState.Count).IsEqualTo(2);
    }
    /// <summary>真正的并发提交不丢失增量。</summary>
    [Test] public async Task ParallelMutationCommitsDoNotLoseUpdatesAsync()
    {
        await using var store = TestCheck.Store(capacity: 64);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++) await store.DispatchAsync(new CounterIntent.Increment());
        })));
        await Assert.That(store.CurrentState.Count).IsEqualTo(800);
    }
    /// <summary>同一锁保护业务守卫和 Busy 转换。</summary>
    [Test] public async Task AtomicGuardRejectsSecondBusinessOperationAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        int calls = 0;
        await using var store = TestCheck.Store(new(async (context, cancellationToken) =>
        {
            if (!context.TryReduce(static state => !state.IsBusy, new CounterMutation.Busy(true))) return;
            Interlocked.Increment(ref calls); entered.TrySetResult();
            try { await release.Task.WaitAsync(cancellationToken); }
            finally { context.TryReduce(static state => state.IsBusy, new CounterMutation.Busy(false)); }
        }));
        Task first = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await store.DispatchAsync(new CounterIntent.Rename("edited"));
            await store.DispatchAsync(new CounterIntent.Run());
            await Assert.That(calls).IsEqualTo(1);
        }
        finally { release.TrySetResult(); await first; }
        await Assert.That(store.CurrentState.IsBusy).IsFalse();
    }
    /// <summary>DispatchAsync 等待 Handler 的业务完成，不只是排队成功。</summary>
    [Test] public async Task DispatchWaitsForAwaitedExternalWorkAsync()
    {
        TaskCompletionSource entered = TestCheck.Signal(), release = TestCheck.Signal();
        await using var store = TestCheck.Store(new(async (context, cancellationToken) =>
        { entered.TrySetResult(); await release.Task.WaitAsync(cancellationToken); context.Reduce(new CounterMutation.Increment()); }));
        Task task = store.DispatchAsync(new CounterIntent.Run()).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        bool completedEarly = task.IsCompleted;
        release.TrySetResult(); await task;
        await Assert.That(completedEarly).IsFalse();
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }
    /// <summary>处理结束后上下文失效，不能被后台任务继续写入。</summary>
    [Test] public async Task CompletedContextCannotCommitAgainAsync()
    {
        IIntentContext<CounterState>? captured = null;
        await using var store = TestCheck.Store(new((context, _) => { captured = context; return ValueTask.CompletedTask; }));
        await store.DispatchAsync(new CounterIntent.Run());
        await TestCheck.ThrowsAsync<InvalidOperationException>(() => { captured!.Reduce(new CounterMutation.Increment()); return Task.CompletedTask; });
        await Assert.That(store.CurrentState.Count).IsEqualTo(0);
    }
    /// <summary>订阅者可以重入派发，状态发布顺序仍然一致。</summary>
    [Test] public async Task ObserverReentrancyDoesNotHoldCommitLockAsync()
    {
        await using var store = TestCheck.Store();
        List<int> observed = [];
        using var subscription = store.States.Subscribe(state =>
        {
            observed.Add(state.Count);
            if (state.Count == 1)
            {
                store.DispatchAsync(new CounterIntent.Increment()).AsTask().GetAwaiter().GetResult();
            }
        });
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(string.Join(",", observed)).IsEqualTo("0,1,2");
    }
}
