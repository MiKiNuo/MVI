using System.Collections.Concurrent;
using System.Collections.Immutable;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证定向请求与同名生成操作共同遵循实例并发契约。</summary>
public sealed class MediatorOperationPolicyTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>不同入口的同名策略错配必须明确拒绝，不能接纳无法推进的等待项。</summary>
    /// <param name="directFirst">是否先从生成的 Queue 入口启动。</param>
    /// <returns>同名策略错配验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MismatchedPolicyNeverLeavesAcceptedQueueWorkWaiting(bool directFirst)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature feature = new(async (_, token) =>
        {
            entered.TrySetResult();
            return await response.Task.WaitAsync(token);
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.DefaultQueue);
        using CancellationTokenSource cancellation = new();
        Task<OperationResult<int>> first = directFirst ? feature.QueueAsync(cancellation.Token) : RequestAsync();
        await entered.Task.WaitAsync(Watchdog);
        Guid runningId = feature.Snapshot.OperationStates["QueueAsync"].RunningId!.Value;
        Task<OperationResult<int>> second = directFirst ? RequestAsync() : feature.QueueAsync(cancellation.Token);
        try
        {
            OperationResult<int> result = await second.WaitAsync(Watchdog);
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].RunningId).IsEqualTo(runningId);
            response.SetResult(1);
            await first.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Rejected);
            await Assert.That(result.Reason).IsEqualTo("OperationConfigurationMismatch");
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].QueuedCount).IsEqualTo(0);
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([1])).IsTrue();
        }
        finally
        {
            response.TrySetResult(1);
            cancellation.Cancel();
            await first.WaitAsync(Watchdog);
            await second.WaitAsync(Watchdog);
        }

        async Task<OperationResult<int>> RequestAsync()
            => (await mediator.SendAsync(new PolicyPortRequest(1), feature.DefaultQueue)).OperationResult!;
    }
    /// <summary>相同 Queue 配置的请求与程序调用共用 FIFO，并在实际启动时采样直接调用输入。</summary>
    /// <returns>混合入口 Queue 完成验证任务。</returns>
    [Test]
    public async Task MatchingQueuePortAndDirectCallsShareFifoAndComplete()
    {
        ConcurrentQueue<int> starts = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature feature = new(async (value, _) =>
        {
            starts.Enqueue(value);
            if (value == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return value;
        });
        Mediator mediator = new();
        RequestPort<PolicyPortRequest, int> port = feature.CreatePort("QueueAsync", OperationConcurrency.Queue, capacity: 2);
        using IDisposable registration = mediator.Register(port);
        Task<RequestResult<int>> first = mediator.SendAsync(new PolicyPortRequest(1), port);
        await entered.Task.WaitAsync(Watchdog);
        feature.SetValue(2);
        Task<OperationResult<int>> direct = feature.QueueAsync();
        Task<RequestResult<int>> third = mediator.SendAsync(new PolicyPortRequest(3), port);
        try
        {
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].QueuedCount).IsEqualTo(2);
            RequestResult<int> full = await mediator.SendAsync(new PolicyPortRequest(5), port);
            await Assert.That(full.OperationResult!.Reason).IsEqualTo("QueueFull");
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].RunningCount).IsEqualTo(1);
            feature.SetValue(4);
            release.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).OperationResult!.Value).IsEqualTo(1);
            await Assert.That((await direct.WaitAsync(Watchdog)).Value).IsEqualTo(4);
            await Assert.That((await third.WaitAsync(Watchdog)).OperationResult!.Value).IsEqualTo(3);
            await Assert.That(starts.SequenceEqual([1, 4, 3])).IsTrue();
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([1, 4, 3])).IsTrue();
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].RunningCount).IsEqualTo(0);
            await Assert.That(feature.Snapshot.OperationStates["QueueAsync"].QueuedCount).IsEqualTo(0);
        }
        finally
        {
            release.TrySetResult();
            await first.WaitAsync(Watchdog);
            await direct.WaitAsync(Watchdog);
            await third.WaitAsync(Watchdog);
        }
    }

    /// <summary>Latest 请求能被同名程序调用取代，忽略取消的旧请求不得提交结果。</summary>
    /// <returns>混合入口 Latest 身份验证任务。</returns>
    [Test]
    public async Task MatchingLatestPortAndDirectCallShareReplacementIdentity()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature feature = new(async (value, _) =>
        {
            if (value == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return value;
        });
        Mediator mediator = new();
        RequestPort<PolicyPortRequest, int> port = feature.CreatePort("LatestAsync", OperationConcurrency.Latest);
        using IDisposable registration = mediator.Register(port);
        Task<RequestResult<int>> first = mediator.SendAsync(new PolicyPortRequest(1), port);
        await entered.Task.WaitAsync(Watchdog);
        try
        {
            feature.SetValue(2);
            OperationResult<int> replacement = await feature.LatestAsync().WaitAsync(Watchdog);
            await Assert.That(replacement.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([2])).IsTrue();
            release.SetResult();
            RequestResult<int> old = await first.WaitAsync(Watchdog);
            await Assert.That(old.OperationResult!.Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([2])).IsTrue();
            await Assert.That(feature.Snapshot.OperationStates["LatestAsync"].RunningCount).IsEqualTo(0);
        }
        finally
        {
            release.TrySetResult();
            await first.WaitAsync(Watchdog);
        }
    }

    /// <summary>Parallel 共享上限与独立身份，单项完成、故障或取消不能清除仍运行的请求。</summary>
    /// <param name="outcome">直接调用的结束方式。</param>
    /// <returns>混合入口 Parallel 隔离验证任务。</returns>
    [Test]
    [Arguments("complete")]
    [Arguments("fault")]
    [Arguments("cancel")]
    public async Task MatchingParallelPortPreservesOtherExecutionOnIndividualExit(string outcome)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> secondResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature feature = new(async (value, token) =>
        {
            if (value == 1) entered.SetResult();
            return await (value == 1 ? firstResponse.Task : secondResponse.Task).WaitAsync(token);
        });
        Mediator mediator = new();
        RequestPort<PolicyPortRequest, int> port = feature.CreatePort("ParallelAsync", OperationConcurrency.Parallel, maxConcurrency: 2);
        using IDisposable registration = mediator.Register(port);
        using CancellationTokenSource cancellation = new();
        Task<RequestResult<int>> first = mediator.SendAsync(new PolicyPortRequest(1), port);
        await entered.Task.WaitAsync(Watchdog);
        Guid firstId = feature.Snapshot.OperationStates["ParallelAsync"].RunningId!.Value;
        feature.SetValue(2);
        Task<OperationResult<int>> second = feature.ParallelAsync(cancellation.Token);
        try
        {
            RequestResult<int> excess = await mediator.SendAsync(new PolicyPortRequest(3), port);
            await Assert.That(excess.OperationResult!.Reason).IsEqualTo("ConcurrencyLimitReached");
            await Assert.That(feature.Snapshot.OperationStates["ParallelAsync"].RunningCount).IsEqualTo(2);
            if (outcome == "cancel") cancellation.Cancel();
            else if (outcome == "fault") secondResponse.SetException(new InvalidOperationException("service fault"));
            else secondResponse.SetResult(2);
            OperationResult<int> secondResult = await second.WaitAsync(Watchdog);
            await Assert.That(secondResult.Kind).IsEqualTo(outcome == "cancel" ? OperationResultKind.Canceled
                : outcome == "fault" ? OperationResultKind.Faulted : OperationResultKind.Completed);
            await Assert.That(feature.Snapshot.OperationStates["ParallelAsync"].RunningIds.SequenceEqual([firstId])).IsTrue();
            await Assert.That(first.IsCompleted).IsFalse();
            firstResponse.SetResult(1);
            RequestResult<int> firstResult = await first.WaitAsync(Watchdog);
            await Assert.That(firstResult.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual(outcome == "complete" ? [2, 1] : [1])).IsTrue();
            await Assert.That(feature.Snapshot.OperationStates["ParallelAsync"].RunningCount).IsEqualTo(0);
        }
        finally
        {
            firstResponse.TrySetResult(1);
            secondResponse.TrySetResult(2);
            await first.WaitAsync(Watchdog);
            await second.WaitAsync(Watchdog);
        }
    }

    /// <summary>同名策略一致但声明界限不同也必须明确拒绝，并保留在途执行。</summary>
    /// <param name="parallel">是否验证 Parallel 上限，否时验证 Queue 等待容量。</param>
    /// <returns>同名界限错配验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MismatchedBoundsAreRejectedWithoutDisturbingRunningWork(bool parallel)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature feature = new(async (_, _) =>
        {
            entered.TrySetResult();
            return await response.Task;
        });
        string name = parallel ? "ParallelAsync" : "QueueAsync";
        Task<OperationResult<int>> execution = parallel ? feature.ParallelAsync() : feature.QueueAsync();
        await entered.Task.WaitAsync(Watchdog);
        Guid runningId = feature.Snapshot.OperationStates[name].RunningId!.Value;
        Mediator mediator = new();
        RequestPort<PolicyPortRequest, int> port = feature.CreatePort(name,
            parallel ? OperationConcurrency.Parallel : OperationConcurrency.Queue,
            capacity: parallel ? 0 : 1, maxConcurrency: parallel ? 1 : 0);
        using IDisposable registration = mediator.Register(port);
        try
        {
            RequestResult<int> mismatch = await mediator.SendAsync(new PolicyPortRequest(3), port).WaitAsync(Watchdog);
            await Assert.That(mismatch.OperationResult!.Kind).IsEqualTo(OperationResultKind.Rejected);
            await Assert.That(mismatch.OperationResult.Reason).IsEqualTo("OperationConfigurationMismatch");
            await Assert.That(feature.Snapshot.OperationStates[name].RunningIds.SequenceEqual([runningId])).IsTrue();
            await Assert.That(feature.Snapshot.OperationStates[name].QueuedCount).IsEqualTo(0);
            response.SetResult(1);
            await execution.WaitAsync(Watchdog);
            RequestResult<int> stillMismatched = await mediator.SendAsync(new PolicyPortRequest(3), port).WaitAsync(Watchdog);
            await Assert.That(stillMismatched.OperationResult!.Reason).IsEqualTo("OperationConfigurationMismatch");
        }
        finally
        {
            response.TrySetResult(1);
            await execution.WaitAsync(Watchdog);
        }
    }
}

internal sealed record PolicyPortRequest(int Value);

internal sealed record PolicyPortState
{
    [Input]
    public int Value { get; init; } = 1;

    public ImmutableList<int> Values { get; init; } = [];
}

internal sealed partial class PolicyPortFeature : Feature<PolicyPortState>
{
    private readonly Func<int, CancellationToken, ValueTask<int>> service;

    internal PolicyPortFeature(Func<int, CancellationToken, ValueTask<int>> service) : base(new())
    {
        this.service = service;
        DefaultQueue = CreateRequestPort<PolicyPortRequest, int>("QueueAsync", null,
            (operation, request) => RunAsync(operation, request.Value));
    }

    internal RequestPort<PolicyPortRequest, int> DefaultQueue { get; }

    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 2)]
    private ValueTask<int> QueueAsync(Operation<PolicyPortState> operation) => RunAsync(operation, operation.Snapshot.Value);

    internal RequestPort<PolicyPortRequest, int> CreatePort(string name, OperationConcurrency concurrency,
        int capacity = 0, int maxConcurrency = 0)
        => CreateRequestPort<PolicyPortRequest, int>(name, null, (operation, request) => RunAsync(operation, request.Value),
            concurrency, capacity, maxConcurrency);

    [Operation(Concurrency = OperationConcurrency.Latest)]
    private ValueTask<int> LatestAsync(Operation<PolicyPortState> operation) => RunAsync(operation, operation.Snapshot.Value);

    [Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private ValueTask<int> ParallelAsync(Operation<PolicyPortState> operation) => RunAsync(operation, operation.Snapshot.Value);

    private async ValueTask<int> RunAsync(Operation<PolicyPortState> operation, int value)
    {
        int result = await service(value, operation.CancellationToken);
        await operation.UpdateAsync(static (state, item) => state with { Values = state.Values.Add(item) }, result);
        return result;
    }
}
