using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过实例、请求、投影与标准工厂入口验证逻辑关闭和真实释放。</summary>
public sealed class FeatureLifetimeTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>外部取消回调内联完成业务等待后仍须保留其尾部的执行和服务范围。</summary>
    /// <returns>表示同步取消续体真实退出验证完成的任务。</returns>
    [Test]
    public async Task InlineExternalCancellationContinuationCannotReleaseCallbackResources()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource callbackTail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource childEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> answer = new();
        using ManualResetEventSlim releaseCallback = new();
        using CancellationTokenSource cancellation = new();
        await using ServiceProvider provider = LifetimeServices.Create(new(async (operation, _, resource) =>
        {
            operation.CancellationToken.Register(() =>
            {
                // 故意同步运行退出续体，再继续使用作用域资源。
                answer.SetResult(1);
                callbackTail.SetResult();
                if (!releaseCallback.Wait(Watchdog)) throw new TimeoutException("外部取消尾部屏障未释放。");
                operation.Track(ChildAsync(resource));
                resource.Use();
            });
            entered.SetResult();
            return await answer.Task;
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> running = feature.RejectAsync(cancellation.Token);
        Task? cancel = null;
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            cancel = Task.Run(cancellation.Cancel);
            await callbackTail.Task.WaitAsync(Watchdog);
            await Assert.That(feature.Snapshot.OperationStates["RejectAsync"].IsRunning).IsTrue();
            CloseResult close = feature.Close();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(running.IsCompleted).IsFalse();
            releaseCallback.Set();
            await cancel.WaitAsync(Watchdog);
            await childEntered.Task.WaitAsync(Watchdog);
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            releaseChild.SetResult();
            await Assert.That((await running.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            releaseCallback.Set();
            releaseChild.TrySetResult();
            if (cancel is not null) await cancel.WaitAsync(Watchdog);
            await running.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task ChildAsync(LifetimeResource resource)
        {
            childEntered.SetResult();
            await releaseChild.Task;
            resource.Use();
        }
    }

    /// <summary>空闲关闭复用唯一票据，仅释放本实例的范围，singleton 由根提供方释放。</summary>
    /// <returns>表示独立范围与重复关闭验证完成的任务。</returns>
    [Test]
    public async Task IdleAndRepeatedCloseReleaseOnlyTheOwnedScope()
    {
        ServiceProvider provider = LifetimeServices.Create(new());
        LifecycleFeature first = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        LifecycleFeature second = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        try
        {
            await Assert.That(ReferenceEquals(first.Resource, second.Resource)).IsFalse();
            await Assert.That(ReferenceEquals(first.Shared, second.Shared)).IsTrue();
            CloseResult close = first.Close();
            await Assert.That(first.IsClosed).IsTrue();
            await Assert.That(second.IsClosed).IsFalse();
            await Assert.That(ReferenceEquals(first.Close(), close)).IsTrue();
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(first.Resource.DisposeCount).IsEqualTo(1);
            await Assert.That(second.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(first.Shared.DisposeCount).IsEqualTo(0);
            await Assert.That((await second.RejectAsync().WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await second.Close().Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(second.Resource.DisposeCount).IsEqualTo(1);
            await provider.DisposeAsync();
            await Assert.That(first.Shared.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            await first.Close().Ticket.Released.WaitAsync(Watchdog);
            await second.Close().Ticket.Released.WaitAsync(Watchdog);
            await provider.DisposeAsync();
        }
    }

    /// <summary>直接构造的功能不释放由调用方传入的服务。</summary>
    /// <returns>表示外部服务归属验证完成的任务。</returns>
    [Test]
    public async Task DirectConstructionDoesNotOwnExternalServices()
    {
        LifetimeResource resource = new();
        LifetimeSingleton singleton = new();
        LifecycleFeature feature = new(resource, singleton, new());
        ReleaseResult released = await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        await Assert.That(released.Succeeded).IsTrue();
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
        resource.Use();
        await resource.DisposeAsync();
        singleton.Dispose();
    }

    /// <summary>逻辑关闭立即拒绝输入与新操作，端口和等待中的旧投影回调失效。</summary>
    /// <returns>表示关闭入口与平台投影验证完成的任务。</returns>
    [Test]
    public async Task CloseRejectsBusinessAndInvalidatesPortsAndQueuedViewCallbacks()
    {
        LifecycleFeature feature = new(new(), new(), new());
        ConcurrentQueue<Action> callbacks = new();
        using LifecycleProjection projection = new(feature, callbacks.Enqueue);
        int notifications = 0;
        projection.PropertyChanged += (_, _) => notifications++;
        feature.SetValue(7);
        RuntimeSnapshot<LifetimeState> committed = feature.Snapshot;
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.Request);
        CloseResult close = feature.Close();
        await Assert.That(() => feature.SetValue(9)).Throws<FeatureClosedException>();
        OperationResult<int> rejected = await feature.RejectAsync().WaitAsync(Watchdog);
        await Assert.That(rejected.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(rejected.Reason).IsEqualTo("Closed");
        await Assert.That(ReferenceEquals(feature.Snapshot, committed)).IsTrue();
        RequestResult<int> unavailable = await mediator.SendAsync(new LifetimeRequest(2), feature.Request);
        await Assert.That(unavailable.Kind).IsEqualTo(RequestResultKind.TargetUnavailable);
        await Assert.That(unavailable.OperationResult).IsNull();
        while (callbacks.TryDequeue(out Action? callback)) callback();
        await Assert.That(notifications).IsEqualTo(0);
        await Assert.That(() => new LifecycleProjection(feature, static action => action())).Throws<FeatureClosedException>();
        await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
    }

    /// <summary>已接纳请求在关闭后保留已提交状态，未合作 IO 真实退出前范围保持可用。</summary>
    /// <returns>表示在途请求与迟到反馈验证完成的任务。</returns>
    [Test]
    public async Task AcceptedRequestKeepsResourcesUntilUncooperativeIoReallyExits()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool lateWriteRejected = false;
        LifetimeWork work = new(async (operation, _, resource) =>
        {
            resource.Use();
            await operation.UpdateAsync(static (state, value) => state with { Value = value }, 17);
            using CancellationTokenRegistration callback = operation.CancellationToken.Register(() => canceled.SetResult());
            entered.SetResult();
            await release.Task;
            resource.Use();
            try
            {
                await operation.UpdateAsync(static (state, value) => state with { Value = value }, 99);
            }
            catch (FeatureClosedException)
            {
                lateWriteRejected = true;
            }

            return 99;
        });
        await using ServiceProvider provider = LifetimeServices.Create(work);
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.Request);
        Task<RequestResult<int>> request = mediator.SendAsync(new LifetimeRequest(1), feature.Request);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult close = feature.Close();
            await canceled.Task.WaitAsync(Watchdog);
            await Assert.That(feature.IsClosed).IsTrue();
            await Assert.That(request.IsCompleted).IsFalse();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(feature.Snapshot.State.Value).IsEqualTo(17);
            release.SetResult();
            RequestResult<int> result = await request.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(RequestResultKind.Responded);
            await Assert.That(result.OperationResult!.Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(result.OperationResult.Exception).IsNull();
            await Assert.That(lateWriteRejected).IsTrue();
            await Assert.That(feature.Snapshot.State.Value).IsEqualTo(17);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            await request.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>取消或超时释放等待不改变后台退出与释放跟踪。</summary>
    /// <returns>表示释放等待隔离验证完成的任务。</returns>
    [Test]
    public async Task ReleaseWaitCancellationAndTimeoutDoNotReleaseActiveResources()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = LifetimeServices.Create(new(async (_, value, resource) =>
        {
            entered.SetResult();
            await release.Task;
            resource.Use();
            return value;
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> operation = feature.RejectAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult close = feature.Close();
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            await Assert.That(async () => await close.Ticket.Released.WaitAsync(cancellation.Token)).Throws<OperationCanceledException>();
            await Assert.That(async () => await close.Ticket.Released.WaitAsync(TimeSpan.Zero)).Throws<TimeoutException>();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            release.SetResult();
            await operation.WaitAsync(Watchdog);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            await operation.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>逻辑关闭和取消期间登记的嵌套子工作继续属于实例的真实退出屏障。</summary>
    /// <returns>表示关闭后嵌套跟踪验证完成的任务。</returns>
    [Test]
    public async Task ClosedOperationCanTrackNestedWorkUntilAllChildrenExit()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = LifetimeServices.Create(new((operation, value, resource) =>
        {
            operation.Track(FirstAsync(operation, resource));
            return ValueTask.FromResult(value);
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> running = feature.RejectAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult close = feature.Close();
            releaseFirst.SetResult();
            await nestedEntered.Task.WaitAsync(Watchdog);
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            releaseNested.SetResult();
            await Assert.That((await running.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseNested.TrySetResult();
            await running.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task FirstAsync(Operation<LifetimeState> operation, LifetimeResource resource)
        {
            entered.SetResult();
            await releaseFirst.Task;
            operation.Track(NestedAsync(resource));
        }

        async Task NestedAsync(LifetimeResource resource)
        {
            nestedEntered.SetResult();
            await releaseNested.Task;
            resource.Use();
        }
    }

    /// <summary>关闭取消回调运行时不持有提交门，并等待其登记的工作后才释放范围。</summary>
    /// <returns>表示取消回调退出屏障验证完成的任务。</returns>
    [Test]
    public async Task CloseWaitsForCancellationCallbackAndTheWorkItTracks()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource childEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseMethod = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseCallback = new();
        LifecycleFeature? feature = null;
        await using ServiceProvider provider = LifetimeServices.Create(new(async (operation, value, resource) =>
        {
            operation.CancellationToken.Register(() =>
            {
                // 另一个线程读取状态、重复关闭、登记工作，证明回调不在 Store 门内执行。
                Task.Run(() => { _ = feature!.Snapshot; feature.Close(); operation.Track(ChildAsync(resource)); })
                    .WaitAsync(Watchdog).GetAwaiter().GetResult();
                callbackEntered.SetResult();
                if (!releaseCallback.Wait(Watchdog)) throw new TimeoutException("取消回调屏障未释放。");
                resource.Use();
            });
            entered.SetResult();
            await releaseMethod.Task;
            return value;
        }));
        feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> running = feature.RejectAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult close = feature.Close();
            await callbackEntered.Task.WaitAsync(Watchdog);
            await childEntered.Task.WaitAsync(Watchdog);
            releaseMethod.SetResult();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            releaseCallback.Set();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            releaseChild.SetResult();
            await Assert.That((await running.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            releaseCallback.Set();
            releaseMethod.TrySetResult();
            releaseChild.TrySetResult();
            await running.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task ChildAsync(LifetimeResource resource)
        {
            childEntered.SetResult();
            await releaseChild.Task;
            resource.Use();
        }
    }

    /// <summary>真实执行集合覆盖所有 Parallel 成员与已被 Latest 取代但尚未退出的旧操作。</summary>
    /// <param name="latest">是否验证 Latest 的被取代执行。</param>
    /// <returns>表示完整执行归属验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CloseRetainsEveryParallelAndSupersededLatestExecution(bool latest)
    {
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = LifetimeServices.Create(new(async (_, value, resource) =>
        {
            (value == 1 ? firstEntered : secondEntered).SetResult();
            await (value == 1 ? releaseFirst : releaseSecond).Task;
            resource.Use();
            return value;
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> first = latest ? feature.LatestAsync() : feature.ParallelAsync();
        await firstEntered.Task.WaitAsync(Watchdog);
        feature.SetValue(2);
        Task<OperationResult<int>> second = latest ? feature.LatestAsync() : feature.ParallelAsync();
        try
        {
            await secondEntered.Task.WaitAsync(Watchdog);
            CloseResult close = feature.Close();
            releaseSecond.SetResult();
            await second.WaitAsync(Watchdog);
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            releaseFirst.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).Kind)
                .IsEqualTo(latest ? OperationResultKind.Superseded : OperationResultKind.Canceled);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseSecond.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>关闭清空 Queue 等待项并结束其调用，不能再启动排队业务。</summary>
    /// <returns>表示队列关闭验证完成的任务。</returns>
    [Test]
    public async Task CloseCompletesQueuedCallsWithoutStartingTheirBusiness()
    {
        ConcurrentQueue<int> starts = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = LifetimeServices.Create(new(async (_, value, resource) =>
        {
            starts.Enqueue(value);
            entered.TrySetResult();
            await release.Task;
            resource.Use();
            return value;
        }));
        LifecycleFeature feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> first = feature.QueueAsync();
        await entered.Task.WaitAsync(Watchdog);
        feature.SetValue(2);
        Task<OperationResult<int>> second = feature.QueueAsync();
        feature.SetValue(3);
        Task<OperationResult<int>> third = feature.QueueAsync();
        try
        {
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].QueuedCount).IsEqualTo(2);
            CloseResult close = feature.Close();
            await Assert.That((await second.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await third.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].QueuedCount).IsEqualTo(0);
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            release.SetResult();
            await first.WaitAsync(Watchdog);
            await close.Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(starts.SequenceEqual([1])).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(first, second, third).WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>业务操作可以关闭自身但不能读取自身的资源释放任务形成等待环。</summary>
    /// <returns>表示直接自等待保护验证完成的任务。</returns>
    [Test]
    public async Task OperationCannotWaitForItsOwnRelease()
    {
        LifecycleFeature? feature = null;
        bool protectedWait = false;
        await using ServiceProvider provider = LifetimeServices.Create(new((_, value, _) =>
        {
            CloseResult close = feature!.Close();
            try
            {
                _ = close.Ticket.Released;
            }
            catch (InvalidOperationException)
            {
                protectedWait = true;
            }

            return ValueTask.FromResult(value);
        }));
        feature = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        await feature.RejectAsync().WaitAsync(Watchdog);
        await Assert.That(protectedWait).IsTrue();
        await Assert.That((await feature.Close().Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
    }
}

internal sealed record LifetimeRequest(int Value);

internal sealed record LifetimeState
{
    [Input]
    public int Value { get; init; } = 1;
    public ImmutableList<int> Values { get; init; } = [];
}

internal sealed partial class LifecycleFeature : Feature<LifetimeState>
{
    private readonly LifetimeWork work;

    public LifecycleFeature(LifetimeResource resource, LifetimeSingleton shared, LifetimeWork work) : base(new())
    {
        Resource = resource;
        Shared = shared;
        this.work = work;
        Request = CreateRequestPort<LifetimeRequest, int>("RejectAsync", null,
            (operation, request) => RunAsync(operation, request.Value));
    }

    internal LifetimeResource Resource { get; }
    internal LifetimeSingleton Shared { get; }
    internal RequestPort<LifetimeRequest, int> Request { get; }

    [Operation]
    private ValueTask<int> RejectAsync(Operation<LifetimeState> operation) => RunAsync(operation, operation.Snapshot.Value);

    [Operation(Concurrency = OperationConcurrency.Latest)]
    private ValueTask<int> LatestAsync(Operation<LifetimeState> operation) => RunAsync(operation, operation.Snapshot.Value);

    [Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private ValueTask<int> ParallelAsync(Operation<LifetimeState> operation) => RunAsync(operation, operation.Snapshot.Value);

    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 2)]
    private ValueTask<int> QueueAsync(Operation<LifetimeState> operation) => RunAsync(operation, operation.Snapshot.Value);

    private async ValueTask<int> RunAsync(Operation<LifetimeState> operation, int value)
    {
        int result = await work.Run(operation, value, Resource);
        await operation.UpdateAsync(static (state, item) => state with { Values = state.Values.Add(item) }, result);
        return result;
    }
}

internal sealed class LifetimeWork(Func<Operation<LifetimeState>, int, LifetimeResource, ValueTask<int>>? run = null)
{
    internal Func<Operation<LifetimeState>, int, LifetimeResource, ValueTask<int>> Run { get; } = run
        ?? (static (_, value, resource) => { resource.Use(); return ValueTask.FromResult(value); });
}

internal sealed class LifetimeResource(Func<ValueTask>? dispose = null) : IAsyncDisposable
{
    private int disposeCount;
    internal int DisposeCount => Volatile.Read(ref disposeCount);

    internal void Use() => ObjectDisposedException.ThrowIf(DisposeCount != 0, this);

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref disposeCount);
        return dispose?.Invoke() ?? ValueTask.CompletedTask;
    }
}

internal sealed class LifetimeSingleton : IDisposable
{
    internal int DisposeCount { get; private set; }
    public void Dispose() => DisposeCount++;
}

internal static class LifetimeServices
{
    internal static ServiceProvider Create(LifetimeWork work, Func<IServiceProvider, LifetimeResource>? resource = null)
    {
        ServiceCollection services = new();
        services.AddScoped(resource ?? (static _ => new LifetimeResource()));
        services.AddSingleton<LifetimeSingleton>();
        services.AddSingleton(work);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}

internal sealed class LifecycleProjection : FeatureProjection<LifetimeState>
{
    internal LifecycleProjection(LifecycleFeature feature, Action<Action> schedule) : base(feature, schedule, ProjectionMode.Coalesce)
        => InitializeProjection();

    protected override void OnSnapshotChanged(LifetimeState previous, LifetimeState current)
    {
    }
}
