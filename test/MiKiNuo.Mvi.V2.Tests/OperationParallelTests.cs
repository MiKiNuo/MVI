using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过生成入口验证有限并行操作的独立输入与统一提交。</summary>
public sealed class OperationParallelTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    /// <summary>验证有限并行拥有独立输入和结果，乱序完成仍保留当前编辑与其他运行身份。</summary>
    /// <returns>表示并行接纳验证结束的任务。</returns>
    [Test]
    public async Task ParallelCallsHaveIndependentStartingInputsAndResults()
    {
        TaskCompletionSource<string> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> secondResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ParallelOperationFeature feature = new(async (input, token) =>
        {
            TaskCompletionSource<string> entered = input.Name == "first" ? firstEntered : secondEntered;
            TaskCompletionSource<int> response = input.Name == "first" ? firstResponse : secondResponse;
            entered.SetResult(input.Name);
            return await response.Task.WaitAsync(token);
        });
        ConcurrentQueue<Action> callbacks = new();
        using ParallelOperationFeature.Projection projection = feature.CreateProjection(callbacks.Enqueue);
        projection.Name = "first";
        Task<OperationResult<int>> first = feature.SubmitAsync();
        await Assert.That(await firstEntered.Task.WaitAsync(Watchdog)).IsEqualTo("first");
        projection.Name = "second";
        Task<OperationResult<int>> second = feature.SubmitAsync();
        RuntimeSnapshot<ParallelOperationState> bothRunning = feature.Snapshot;
        try
        {
            await Assert.That(second.IsCompleted).IsFalse();
            await Assert.That(await secondEntered.Task.WaitAsync(Watchdog)).IsEqualTo("second");
            await Assert.That(bothRunning.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(2);
            Guid firstId = bothRunning.OperationStates["SubmitAsync"].RunningIds[0];
            Guid secondId = bothRunning.OperationStates["SubmitAsync"].RunningIds[1];
            OperationResult<int> rejected = await feature.SubmitAsync();
            await Assert.That(rejected.Kind).IsEqualTo(OperationResultKind.Rejected);
            await Assert.That(rejected.Reason).IsEqualTo("ConcurrencyLimitReached");
            await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.SequenceEqual([firstId, secondId])).IsTrue();

            projection.Name = "edited during IO";
            secondResponse.SetResult(7);
            OperationResult<int> secondResult = await second.WaitAsync(Watchdog);
            await Assert.That(secondResult.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(secondResult.Value).IsEqualTo(7);
            await Assert.That(secondResult.OperationId).IsEqualTo(secondId);
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(feature.Snapshot.State.Total).IsEqualTo(7);
            await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.SequenceEqual([firstId])).IsTrue();
            Drain(callbacks);
            await Assert.That(projection.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsTrue();
            await Assert.That(projection.Total).IsEqualTo(7);

            firstResponse.SetResult(3);
            OperationResult<int> firstResult = await first.WaitAsync(Watchdog);
            await Assert.That(firstResult.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(firstResult.Value).IsEqualTo(3);
            await Assert.That(firstResult.OperationId).IsEqualTo(firstId);
            await Assert.That(firstResult.OperationId != secondResult.OperationId).IsTrue();
            await Assert.That(feature.Snapshot.State.Total).IsEqualTo(10);
            await Assert.That(feature.Snapshot.State.Name).IsEqualTo("edited during IO");
            await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(0);
            await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningId).IsNull();
            await Assert.That(bothRunning.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(2);
            await Assert.That(bothRunning.State.Total).IsEqualTo(0);
            await Assert.That(feature.Snapshot.Version > bothRunning.Version).IsTrue();
            Drain(callbacks);
            await Assert.That(projection.Total).IsEqualTo(10);
            await Assert.That(projection.Name).IsEqualTo("edited during IO");
            await Assert.That(projection.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsFalse();
        }
        finally
        {
            firstResponse.TrySetResult(3);
            secondResponse.TrySetResult(7);
        }

    }

    /// <summary>验证单项取消或服务故障只结束自己的执行，其他有效反馈继续提交。</summary>
    /// <param name="cancel">是否以协作取消结束第一项。</param>
    /// <returns>表示执行隔离验证结束的任务。</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CancellationAndFaultAreIsolatedFromOtherExecutions(bool cancel)
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> secondResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("第一项服务故障。");
        ParallelOperationFeature feature = new(async (input, token) =>
        {
            if (input.Name == "first")
            {
                firstEntered.SetResult();
                return await firstResponse.Task.WaitAsync(token);
            }

            secondEntered.SetResult();
            return await secondResponse.Task.WaitAsync(token);
        });
        feature.SetName("first");
        Task<OperationResult<int>> first = feature.SubmitAsync(cancellation.Token);
        await firstEntered.Task.WaitAsync(Watchdog);
        feature.SetName("second");
        Task<OperationResult<int>> second = feature.SubmitAsync();
        await secondEntered.Task.WaitAsync(Watchdog);
        if (cancel)
        {
            cancellation.Cancel();
        }
        else
        {
            firstResponse.SetException(failure);
        }

        OperationResult<int> firstResult = await first.WaitAsync(Watchdog);
        await Assert.That(firstResult.Kind).IsEqualTo(cancel ? OperationResultKind.Canceled : OperationResultKind.Faulted);
        await Assert.That(firstResult.HasValue).IsFalse();
        if (!cancel)
        {
            await Assert.That(ReferenceEquals(firstResult.Exception, failure)).IsTrue();
        }

        await Assert.That(second.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(1);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.Contains(firstResult.OperationId)).IsFalse();
        secondResponse.SetResult(9);
        OperationResult<int> secondResult = await second.WaitAsync(Watchdog);
        await Assert.That(secondResult.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(secondResult.Value).IsEqualTo(9);
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(9);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsFalse();
    }

    /// <summary>验证未接纳尝试的验证失败或预取消不会清空已有执行身份。</summary>
    /// <returns>表示拒绝反馈与运行身份验证结束的任务。</returns>
    [Test]
    public async Task ValidationFailureAndPreCancellationPreserveExistingExecution()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        ParallelOperationFeature feature = new(async (_, token) =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult();
            return await response.Task.WaitAsync(token);
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await entered.Task.WaitAsync(Watchdog);
        Guid runningId = feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.Single();
        feature.SetName(string.Empty);
        OperationResult<int> invalid = await feature.SubmitAsync();
        await Assert.That(invalid.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(invalid.Reason).IsEqualTo("ValidationFailed");
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.SequenceEqual([runningId])).IsTrue();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].LastAttemptId).IsEqualTo(invalid.OperationId);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        OperationResult<int> canceled = await feature.SubmitAsync(cancellation.Token);
        await Assert.That(canceled.Kind).IsEqualTo(OperationResultKind.Canceled);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningIds.SequenceEqual([runningId])).IsTrue();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].LastAttemptId).IsEqualTo(canceled.OperationId);
        await Assert.That(calls).IsEqualTo(1);
        response.SetResult(5);
        OperationResult<int> completed = await execution.WaitAsync(Watchdog);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.OperationId).IsEqualTo(runningId);
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(5);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo(string.Empty);
    }

    /// <summary>验证竞争启动不能越过上限，并发反馈不能丢失累计状态。</summary>
    /// <returns>表示并发接纳与提交验证结束的任务。</returns>
    [Test]
    public async Task CompetingAdmissionsAndFeedbackStayWithinBoundAndPreserveAllUpdates()
    {
        TaskCompletionSource launch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        ParallelOperationFeature feature = new(async (_, token) =>
        {
            if (Interlocked.Increment(ref calls) == 2)
            {
                bothEntered.SetResult();
            }

            return await response.Task.WaitAsync(token);
        });
        feature.SetName("batch");
        Task<Task<OperationResult<int>>>[] admissions = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
        {
            await launch.Task;
            return feature.SubmitAsync();
        })).ToArray();
        launch.SetResult();
        Task<OperationResult<int>>[] executions = await Task.WhenAll(admissions).WaitAsync(Watchdog);
        await bothEntered.Task.WaitAsync(Watchdog);
        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(2);
        response.SetResult(1);
        OperationResult<int>[] results = await Task.WhenAll(executions).WaitAsync(Watchdog);
        await Assert.That(results.Count(static result => result.Kind == OperationResultKind.Completed)).IsEqualTo(2);
        await Assert.That(results.Count(static result => result.Kind == OperationResultKind.Rejected && result.Reason == "ConcurrencyLimitReached")).IsEqualTo(10);
        await Assert.That(results.Select(static result => result.OperationId).Distinct().Count()).IsEqualTo(12);
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(2);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(0);
    }

    /// <summary>验证取消或故障后的所属嵌套工作真实退出前仍占据名额。</summary>
    /// <param name="mode">第一项执行的结束方式。</param>
    /// <returns>表示真实退出屏障与名额验证结束的任务。</returns>
    [Test]
    [Arguments("completed")]
    [Arguments("canceled")]
    [Arguments("faulted")]
    public async Task CapacityIsHeldUntilTrackedAndNestedWorkReallyExit(string mode)
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource childEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("第一项业务故障。");
        TrackedParallelOperationFeature feature = new(mode, failure, childEntered, nestedEntered,
            releaseChild.Task, releaseNested.Task, response.Task);
        feature.SetName("first");
        Task<OperationResult<int>> first = feature.SubmitAsync(cancellation.Token);
        await childEntered.Task.WaitAsync(Watchdog);
        feature.SetName("second");
        Task<OperationResult<int>> second = feature.SubmitAsync();
        if (mode == "canceled")
        {
            cancellation.Cancel();
        }

        releaseChild.SetResult();
        await nestedEntered.Task.WaitAsync(Watchdog);
        await Assert.That(first.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(2);
        OperationResult<int> rejected = await feature.SubmitAsync();
        await Assert.That(rejected.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(rejected.Reason).IsEqualTo("ConcurrencyLimitReached");
        releaseNested.SetResult();
        OperationResult<int> firstResult = await first.WaitAsync(Watchdog);
        OperationResultKind expected = mode == "faulted" ? OperationResultKind.Faulted
            : mode == "canceled" ? OperationResultKind.Canceled : OperationResultKind.Completed;
        await Assert.That(firstResult.Kind).IsEqualTo(expected);
        if (mode == "faulted")
        {
            await Assert.That(ReferenceEquals(firstResult.Exception, failure)).IsTrue();
        }

        await Assert.That(second.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(1);
        feature.SetName("replacement");
        Task<OperationResult<int>> replacement = feature.SubmitAsync();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(2);
        response.SetResult(3);
        OperationResult<int>[] remaining = await Task.WhenAll(second, replacement).WaitAsync(Watchdog);
        await Assert.That(remaining.All(static result => result.Kind == OperationResultKind.Completed)).IsTrue();
        await Assert.That(feature.Snapshot.State.Total).IsEqualTo(6);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningCount).IsEqualTo(0);
    }

    /// <summary>验证直接调用受保护接纳入口也明确拒绝非法配置，并保留已有执行。</summary>
    /// <param name="concurrency">待验证的策略数值。</param>
    /// <param name="maxConcurrency">待验证的上限。</param>
    /// <returns>表示直接入口配置验证结束的任务。</returns>
    [Test]
    [Arguments(1, 0)]
    [Arguments(1, -1)]
    [Arguments(99, 2)]
    [Arguments(0, 2)]
    [Arguments(0, -1)]
    public async Task DirectEntryRejectsInvalidConfigurationWithoutDisturbingExistingWork(int concurrency, int maxConcurrency)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        DirectConfigurationFeature feature = new(async () =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult();
            return await response.Task;
        });
        Task<OperationResult<int>> execution = feature.ExecuteAsync(OperationConcurrency.Parallel, 2);
        await entered.Task.WaitAsync(Watchdog);
        Guid runningId = feature.Snapshot.OperationStates["ExecuteAsync"].RunningIds.Single();
        OperationResult<int> invalid = await feature.ExecuteAsync((OperationConcurrency)concurrency, maxConcurrency);
        await Assert.That(invalid.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(invalid.Reason).IsEqualTo("InvalidConcurrency");
        await Assert.That(feature.Snapshot.OperationStates["ExecuteAsync"].Reason).IsEqualTo("InvalidConcurrency");
        await Assert.That(feature.Snapshot.OperationStates["ExecuteAsync"].RunningIds.SequenceEqual([runningId])).IsTrue();
        await Assert.That(calls).IsEqualTo(1);
        response.SetResult(3);
        OperationResult<int> completed = await execution.WaitAsync(Watchdog);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.OperationStates["ExecuteAsync"].RunningCount).IsEqualTo(0);
    }

    private static void Drain(ConcurrentQueue<Action> callbacks)
    {
        while (callbacks.TryDequeue(out Action? callback))
        {
            callback();
        }
    }
}

internal sealed record ParallelOperationState
{
    [Input]
    public string Name { get; init; } = string.Empty;

    public int Total { get; init; }
}

internal sealed partial class ParallelOperationFeature(Func<ParallelOperationState, CancellationToken, ValueTask<int>> service)
    : Feature<ParallelOperationState>(new())
{
    [Operation(Validate = nameof(CanSubmit), Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private async ValueTask<int> SubmitAsync(Operation<ParallelOperationState> operation)
    {
        int result = await service(operation.Snapshot, operation.CancellationToken);
        await operation.UpdateAsync(static (state, value) => state with { Total = state.Total + value }, result);
        return result;
    }

    private static bool CanSubmit(ParallelOperationState state) => !string.IsNullOrWhiteSpace(state.Name);
}

internal sealed partial class TrackedParallelOperationFeature(string mode, Exception failure,
    TaskCompletionSource childEntered, TaskCompletionSource nestedEntered, Task releaseChild, Task releaseNested, Task<int> response)
    : Feature<ParallelOperationState>(new())
{
    [Operation(Concurrency = OperationConcurrency.Parallel, MaxConcurrency = 2)]
    private async ValueTask<int> SubmitAsync(Operation<ParallelOperationState> operation)
    {
        if (operation.Snapshot.Name == "first")
        {
            operation.Track(Child());
            if (mode == "faulted")
            {
                throw failure;
            }

            return 1;
        }

        int result = await response.WaitAsync(operation.CancellationToken);
        await operation.UpdateAsync(static (state, value) => state with { Total = state.Total + value }, result);
        return result;

        async Task Child()
        {
            childEntered.SetResult();
            await releaseChild;
            operation.Track(Nested());
        }

        async Task Nested()
        {
            nestedEntered.SetResult();
            await releaseNested;
            operation.Track(Task.CompletedTask);
        }
    }
}

internal sealed partial class DirectConfigurationFeature(Func<ValueTask<int>> service) : Feature<ParallelOperationState>(new())
{
    internal Task<OperationResult<int>> ExecuteAsync(OperationConcurrency concurrency, int maxConcurrency)
        => DispatchOperation("ExecuteAsync", null, _ => service(), CancellationToken.None, concurrency, maxConcurrency);
}
