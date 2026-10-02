using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开操作入口验证有界顺序队列的接纳与完成契约。</summary>
public sealed class OperationQueueTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>验证持提交门校验期间跨线程请求排队取消不会形成等待环，取消项不会随后执行。</summary>
    /// <returns>表示取消回调与提交门协调验证完成的任务。</returns>
    [Test]
    public async Task QueuedCancellationFromValidationDoesNotBlockCommitGate()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        AtomicQueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return index;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        Task<OperationResult<int>> canceled = feature.SaveAsync(cancellation.Token);
        AtomicQueueOperationFeature.Rule = _ =>
        {
            if (!Task.Run(cancellation.Cancel).Wait(Watchdog))
            {
                throw new TimeoutException("取消回调不能等待正在校验的提交门。");
            }

            return true;
        };
        try
        {
            release.SetResult();
            await first.WaitAsync(Watchdog);
            await Assert.That((await second.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That((await canceled.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(calls).IsEqualTo(2);
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(0);
        }
        finally
        {
            release.TrySetResult();
            AtomicQueueOperationFeature.Rule = static _ => true;
        }
    }

    /// <summary>验证较大有限队列连续校验拒绝时仍为每项提供结果，不耗尽同步调用栈。</summary>
    /// <returns>表示连续拒绝排空验证完成的任务。</returns>
    [Test]
    public async Task LongQueueReturnsEveryResultAfterConsecutiveValidationRejections()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        LargeQueueOperationFeature feature = new(async _ =>
        {
            Interlocked.Increment(ref calls);
            entered.SetResult();
            await release.Task;
            return 1;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>>[] queued = Enumerable.Range(0, 4096).Select(_ => feature.SaveAsync()).ToArray();
        feature.SetName(string.Empty);
        release.SetResult();
        await first.WaitAsync(Watchdog);
        OperationResult<int>[] results = await Task.WhenAll(queued).WaitAsync(Watchdog);
        await Assert.That(results.All(static result => result.Kind == OperationResultKind.Rejected && result.Reason == "ValidationFailed")).IsTrue();
        await Assert.That(calls).IsEqualTo(1);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(0);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].IsRunning).IsFalse();
    }

    /// <summary>验证排队项开始校验与并发编辑竞争时，服务仍收到通过校验的同一快照。</summary>
    /// <returns>表示排队启动原子性验证完成的任务。</returns>
    [Test]
    public async Task QueuedValidationAndServiceInputAreAtomicAgainstConcurrentEditing()
    {
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource validationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource inputAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseValidation = new();
        string? secondInput = null;
        int calls = 0;
        AtomicQueueOperationFeature feature = new(async operation =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
            }
            else
            {
                secondInput = operation.Snapshot.Name;
            }

            return index;
        });
        feature.SetName("validated");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await firstEntered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        AtomicQueueOperationFeature.Rule = state =>
        {
            validationEntered.SetResult();
            if (!releaseValidation.Wait(Watchdog))
            {
                throw new TimeoutException("队列验证测试未释放同步信号。");
            }

            return state.Name == "validated";
        };
        try
        {
            releaseFirst.SetResult();
            await validationEntered.Task.WaitAsync(Watchdog);
            Task input = Task.Run(() =>
            {
                inputAttempted.SetResult();
                feature.SetName("unvalidated edit");
            });
            await inputAttempted.Task.WaitAsync(Watchdog);
            releaseValidation.Set();
            await input.WaitAsync(Watchdog);
            await first.WaitAsync(Watchdog);
            await Assert.That((await second.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(secondInput).IsEqualTo("validated");
            await Assert.That(feature.Snapshot.State.Name).IsEqualTo("unvalidated edit");
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseValidation.Set();
            AtomicQueueOperationFeature.Rule = static _ => true;
        }
    }

    /// <summary>验证启动校验异常或重入不改变业务状态，并继续处理后续已接纳项。</summary>
    /// <param name="reenter">是否从纯校验重入输入入口。</param>
    /// <returns>表示队列校验故障隔离验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task QueuedValidationFaultDoesNotStopFollowingCalls(bool reenter)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        int validations = 0;
        AtomicQueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return index;
        });
        feature.SetName("unchanged");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> faulted = feature.SaveAsync();
        Task<OperationResult<int>> survivor = feature.SaveAsync();
        AtomicQueueOperationFeature.Rule = _ =>
        {
            if (Interlocked.Increment(ref validations) == 1)
            {
                if (reenter)
                {
                    feature.SetName("nested write");
                }

                throw new InvalidOperationException("排队项校验故障");
            }

            return true;
        };
        try
        {
            release.SetResult();
            await first.WaitAsync(Watchdog);
            OperationResult<int> failure = await faulted.WaitAsync(Watchdog);
            await Assert.That(failure.Kind).IsEqualTo(OperationResultKind.Faulted);
            await Assert.That(failure.Reason).IsEqualTo("ValidationFault");
            await Assert.That(failure.Exception is InvalidOperationException).IsTrue();
            await Assert.That((await survivor.WaitAsync(Watchdog)).Value).IsEqualTo(2);
            await Assert.That(feature.Snapshot.State.Name).IsEqualTo("unchanged");
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(0);
        }
        finally
        {
            release.TrySetResult();
            AtomicQueueOperationFeature.Rule = static _ => true;
        }
    }

    /// <summary>验证完成展示同步重入的新调用不能越过此前等待的调用。</summary>
    /// <returns>表示完成与重入接纳顺序验证完成的任务。</returns>
    [Test]
    public async Task ReentrantDisplayCannotOvertakeAlreadyQueuedCall()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        QueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return index;
        });
        feature.SetName("ready");
        using QueueOperationFeature.Projection projection = feature.CreateProjection(static action => action());
        Task<OperationResult<int>>? reentered = null;
        projection.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "Snapshot" && reentered is null
                && projection.Snapshot.OperationStates.TryGetValue("SaveAsync", out OperationState? state)
                && state.LastResult == OperationResultKind.Completed && !state.IsRunning && state.QueuedCount == 1)
            {
                reentered = feature.SaveAsync();
            }
        };
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        release.SetResult();
        await first.WaitAsync(Watchdog);
        await Assert.That((await second.WaitAsync(Watchdog)).Value).IsEqualTo(2);
        await Assert.That(reentered is not null).IsTrue();
        await Assert.That((await reentered!.WaitAsync(Watchdog)).Value).IsEqualTo(3);
    }

    /// <summary>验证等待项只在真正启动时校验并使用当时输入，入队状态不提供旧授权。</summary>
    /// <param name="validAtStart">实际启动时输入是否有效。</param>
    /// <returns>表示启动时验证与采样验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task QueuedCallValidatesAndSamplesStateAtActualStart(bool validAtStart)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        string? secondInput = null;
        int calls = 0;
        QueueOperationFeature feature = new(async operation =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.SetResult();
                await release.Task;
                await operation.UpdateAsync(static (state, value) => state with { Result = state.Result + value }, 10);
            }
            else
            {
                secondInput = operation.Snapshot.Name;
                await Assert.That(operation.Snapshot.Result).IsEqualTo(10);
                await operation.UpdateAsync(static (state, value) => state with { Result = state.Result + value }, 20);
            }

            return calls;
        });
        feature.SetName("first");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        feature.SetName(validAtStart ? string.Empty : "old authorization");
        Task<OperationResult<int>> second = feature.SaveAsync();
        await Assert.That(second.IsCompleted).IsFalse();
        feature.SetName(validAtStart ? "current validated input" : string.Empty);
        release.SetResult();
        await first.WaitAsync(Watchdog);
        OperationResult<int> result = await second.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(validAtStart ? OperationResultKind.Completed : OperationResultKind.Rejected);
        await Assert.That(result.Reason).IsEqualTo(validAtStart ? null : "ValidationFailed");
        await Assert.That(calls).IsEqualTo(validAtStart ? 2 : 1);
        await Assert.That(secondInput).IsEqualTo(validAtStart ? "current validated input" : null);
        RuntimeSnapshot<OperationTestState> completed = feature.Snapshot;
        await Assert.That(completed.State.Name).IsEqualTo(validAtStart ? "current validated input" : string.Empty);
        await Assert.That(completed.State.Result).IsEqualTo(validAtStart ? 30 : 10);
        await Assert.That(completed.OperationStates["SaveAsync"].LastResult).IsEqualTo(result.Kind);
        await Assert.That(completed.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(0);
        await Assert.That(completed.OperationStates["SaveAsync"].IsRunning).IsFalse();
    }

    /// <summary>验证业务失败值和服务故障都有明确结果并继续处理其他已接纳项。</summary>
    /// <returns>表示队列故障隔离验证完成的任务。</returns>
    [Test]
    public async Task ServiceFaultAndBusinessFailureDoNotLoseAcceptedCalls()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("保存服务故障");
        int calls = 0;
        QueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                entered.SetResult();
                await release.Task;
                throw failure;
            }

            return index == 2 ? -1 : 3;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        Task<OperationResult<int>> third = feature.SaveAsync();
        release.SetResult();
        OperationResult<int> faulted = await first.WaitAsync(Watchdog);
        OperationResult<int> businessFailure = await second.WaitAsync(Watchdog);
        OperationResult<int> completed = await third.WaitAsync(Watchdog);
        await Assert.That(faulted.Kind).IsEqualTo(OperationResultKind.Faulted);
        await Assert.That(ReferenceEquals(faulted.Exception, failure)).IsTrue();
        await Assert.That(businessFailure.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(businessFailure.HasValue).IsTrue();
        await Assert.That(businessFailure.Value).IsEqualTo(-1);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.Value).IsEqualTo(3);
        await Assert.That(calls).IsEqualTo(3);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].LastAttemptId).IsEqualTo(completed.OperationId);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].IsRunning).IsFalse();
    }

    /// <summary>验证运行项取消后必须等已登记及嵌套工作真正退出，才能启动下一项。</summary>
    /// <returns>表示取消与真实工作排空验证完成的任务。</returns>
    [Test]
    public async Task RunningCancellationDrainsTrackedAndNestedWorkBeforeNextStart()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource childEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        Exception? lateFeedback = null;
        QueueOperationFeature feature = new(async operation =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index != 1)
            {
                secondEntered.SetResult();
                return index;
            }

            await operation.UpdateAsync(static (state, value) => state with { Result = value }, 7);
            operation.Track(Child());
            return 1;

            async Task Child()
            {
                childEntered.SetResult();
                await releaseChild.Task;
                operation.Track(Nested());
            }

            async Task Nested()
            {
                nestedEntered.SetResult();
                await releaseNested.Task;
                try
                {
                    await operation.UpdateAsync(static (state, value) => state with { Result = value }, 99);
                }
                catch (OperationCanceledException exception)
                {
                    lateFeedback = exception;
                }
            }
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync(cancellation.Token);
        await childEntered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        try
        {
            cancellation.Cancel();
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(secondEntered.Task.IsCompleted).IsFalse();
            releaseChild.SetResult();
            await nestedEntered.Task.WaitAsync(Watchdog);
            await Assert.That(secondEntered.Task.IsCompleted).IsFalse();
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(1);
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].IsRunning).IsTrue();
            releaseNested.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await second.WaitAsync(Watchdog)).Value).IsEqualTo(2);
            await Assert.That(lateFeedback is OperationCanceledException).IsTrue();
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].IsRunning).IsFalse();
        }
        finally
        {
            releaseChild.TrySetResult();
            releaseNested.TrySetResult();
        }
    }

    /// <summary>验证排队取消及时给出结果并释放名额，取消项不会在运行项退出后启动。</summary>
    /// <returns>表示排队取消验证完成的任务。</returns>
    [Test]
    public async Task QueuedCancellationReleasesCapacityBeforeRunningWorkExits()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        QueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls);
            if (index == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return index;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered.Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> canceled = feature.SaveAsync(cancellation.Token);
        Task<OperationResult<int>> survivor = feature.SaveAsync();
        try
        {
            cancellation.Cancel();
            OperationResult<int> result = await canceled.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(1);
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].LastAttemptId).IsEqualTo(result.OperationId);
            Task<OperationResult<int>> replacement = feature.SaveAsync();
            await Assert.That(replacement.IsCompleted).IsFalse();
            await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(2);
            release.SetResult();
            await first.WaitAsync(Watchdog);
            await Assert.That((await survivor.WaitAsync(Watchdog)).Value).IsEqualTo(2);
            await Assert.That((await replacement.WaitAsync(Watchdog)).Value).IsEqualTo(3);
            await Assert.That(calls).IsEqualTo(3);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    /// <summary>验证容量仅计算等待项，满载拒绝不执行服务且已接纳调用按顺序启动。</summary>
    /// <returns>表示顺序与容量验证完成的任务。</returns>
    [Test]
    public async Task QueueUsesWaitingCapacityAndStartsAcceptedCallsInOrder()
    {
        TaskCompletionSource[] entered = Enumerable.Range(0, 3)
            .Select(static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        TaskCompletionSource[] release = Enumerable.Range(0, 3)
            .Select(static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        int calls = 0;
        QueueOperationFeature feature = new(async _ =>
        {
            int index = Interlocked.Increment(ref calls) - 1;
            entered[index].SetResult();
            await release[index].Task;
            return index + 1;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> first = feature.SaveAsync();
        await entered[0].Task.WaitAsync(Watchdog);
        Task<OperationResult<int>> second = feature.SaveAsync();
        Task<OperationResult<int>> third = feature.SaveAsync();
        OperationState waiting = feature.Snapshot.OperationStates["SaveAsync"];
        OperationResult<int> full = await feature.SaveAsync().WaitAsync(Watchdog);
        await Assert.That(full.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(full.Reason).IsEqualTo("QueueFull");
        await Assert.That(waiting.QueuedCount).IsEqualTo(2);
        await Assert.That(waiting.IsRunning).IsTrue();
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].RunningId).IsEqualTo(waiting.RunningId);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(2);
        await Assert.That(calls).IsEqualTo(1);
        await Assert.That(second.IsCompleted).IsFalse();
        await Assert.That(third.IsCompleted).IsFalse();
        release[0].SetResult();
        await entered[1].Task.WaitAsync(Watchdog);
        await Assert.That((await first.WaitAsync(Watchdog)).Value).IsEqualTo(1);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(1);
        release[1].SetResult();
        await entered[2].Task.WaitAsync(Watchdog);
        await Assert.That((await second.WaitAsync(Watchdog)).Value).IsEqualTo(2);
        release[2].SetResult();
        OperationResult<int> completed = await third.WaitAsync(Watchdog);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.Value).IsEqualTo(3);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].QueuedCount).IsEqualTo(0);
        await Assert.That(feature.Snapshot.OperationStates["SaveAsync"].IsRunning).IsFalse();
    }
}

internal sealed partial class QueueOperationFeature(Func<Operation<OperationTestState>, ValueTask<int>> service)
    : Feature<OperationTestState>(new())
{
    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 2, Validate = nameof(CanSave))]
    private ValueTask<int> SaveAsync(Operation<OperationTestState> operation) => service(operation);

    private static bool CanSave(OperationTestState state) => state.Name.Length != 0;
}

internal sealed partial class AtomicQueueOperationFeature(Func<Operation<OperationTestState>, ValueTask<int>> service)
    : Feature<OperationTestState>(new())
{
    internal static Func<OperationTestState, bool> Rule { get; set; } = static _ => true;

    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 2, Validate = nameof(CanSave))]
    private ValueTask<int> SaveAsync(Operation<OperationTestState> operation) => service(operation);

    private static bool CanSave(OperationTestState state) => Rule(state);
}

internal sealed partial class LargeQueueOperationFeature(Func<Operation<OperationTestState>, Task<int>> service)
    : Feature<OperationTestState>(new())
{
    [Operation(Concurrency = OperationConcurrency.Queue, Capacity = 4096, Validate = nameof(CanSave))]
    private Task<int> SaveAsync(Operation<OperationTestState> operation) => service(operation);

    private static bool CanSave(OperationTestState state) => state.Name.Length != 0;
}
