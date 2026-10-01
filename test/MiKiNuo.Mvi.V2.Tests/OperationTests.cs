using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证公开操作入口的验证、反馈与真实完成行为。</summary>
public sealed class OperationTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>验证服务通过关联令牌响应本次执行取消时仍报告协作取消。</summary>
    /// <returns>表示关联令牌取消验证完成的任务。</returns>
    [Test]
    public async Task LinkedServiceTokenCancellationIsCooperative()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        OperationFeature feature = new(async operation =>
        {
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(operation.CancellationToken);
            Task waiting = release.Task.WaitAsync(linked.Token);
            entered.SetResult();
            await waiting;
            return 1;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync(cancellation.Token);
        await entered.Task.WaitAsync(Watchdog);
        cancellation.Cancel();
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Canceled);
        await Assert.That(result.HasValue).IsFalse();
        await Assert.That(result.Exception).IsNull();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].LastResult).IsEqualTo(OperationResultKind.Canceled);
    }

    /// <summary>验证结束的旧上下文不能更新后续同名执行或清除其运行状态。</summary>
    /// <returns>表示操作身份隔离验证完成的任务。</returns>
    [Test]
    public async Task FinishedContextCannotAffectNewExecutionWithSameName()
    {
        Operation<OperationTestState>? previous = null;
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        OperationFeature feature = new(async operation =>
        {
            if (previous is null)
            {
                previous = operation;
                return 1;
            }

            entered.SetResult();
            await release.Task;
            return 2;
        });
        feature.SetName("ready");
        await feature.SubmitAsync();
        Task<OperationResult<int>> next = feature.SubmitAsync();
        await entered.Task.WaitAsync(Watchdog);
        RuntimeSnapshot<OperationTestState> running = feature.Snapshot;
        Exception? failure = null;
        try
        {
            await previous!.UpdateAsync(static (state, value) => state with { Result = value }, 99);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        await Assert.That(failure is InvalidOperationException).IsTrue();
        await Assert.That(ReferenceEquals(feature.Snapshot, running)).IsTrue();
        release.SetResult();
        OperationResult<int> completed = await next.WaitAsync(Watchdog);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.Value).IsEqualTo(2);
    }

    /// <summary>验证只有本次执行已请求取消，所属子工作的取消才报告协作取消。</summary>
    /// <param name="unrelatedToken">是否使用无关令牌取消所属工作。</param>
    /// <returns>表示子工作取消关联验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TrackedTaskCancellationRequiresOperationToken(bool unrelatedToken)
    {
        using CancellationTokenSource cancellation = new();
        using CancellationTokenSource unrelated = new();
        unrelated.Cancel();
        OperationFeature feature = new(operation =>
        {
            if (!unrelatedToken)
            {
                cancellation.Cancel();
            }

            operation.Track(Task.FromCanceled(unrelatedToken ? unrelated.Token : cancellation.Token));
            return ValueTask.FromResult(1);
        });
        feature.SetName("ready");
        OperationResult<int> result = await feature.SubmitAsync(cancellation.Token);
        await Assert.That(result.Kind).IsEqualTo(unrelatedToken ? OperationResultKind.Faulted : OperationResultKind.Canceled);
        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>验证启动验证与并发输入竞争时服务使用通过验证的同一输入。</summary>
    /// <returns>表示原子启动验证完成的任务。</returns>
    [Test]
    public async Task ValidationAndStartingInputAreAtomicAgainstConcurrentEditing()
    {
        TaskCompletionSource validationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource inputAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseValidation = new();
        TaskCompletionSource<string> serviceInput = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AtomicValidationFeature feature = new(operation =>
        {
            serviceInput.SetResult(operation.Snapshot.Name);
            return ValueTask.FromResult(1);
        });
        feature.SetName("validated");
        AtomicValidationFeature.Rule = state =>
        {
            validationEntered.SetResult();
            if (!releaseValidation.Wait(Watchdog))
            {
                throw new TimeoutException("验证测试未释放同步信号。");
            }

            return state.Name == "validated";
        };
        try
        {
            Task<OperationResult<int>> execution = Task.Run(() => feature.SubmitAsync());
            await validationEntered.Task.WaitAsync(Watchdog);
            Task input = Task.Run(() =>
            {
                inputAttempted.SetResult();
                feature.SetName("unvalidated new input");
            });
            await inputAttempted.Task.WaitAsync(Watchdog);
            releaseValidation.Set();
            await input.WaitAsync(Watchdog);
            await Assert.That(await serviceInput.Task.WaitAsync(Watchdog)).IsEqualTo("validated");
            await Assert.That((await execution.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(feature.Snapshot.State.Name).IsEqualTo("unvalidated new input");
        }
        finally
        {
            releaseValidation.Set();
            AtomicValidationFeature.Rule = static _ => true;
        }
    }

    /// <summary>验证验证规则异常或重入不能启动服务或改变原业务状态。</summary>
    /// <param name="reenter">是否从纯验证重入状态入口。</param>
    /// <returns>表示验证规则故障验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ValidationFaultAndReentryPreserveBusinessState(bool reenter)
    {
        int calls = 0;
        AtomicValidationFeature feature = new(_ =>
        {
            calls++;
            return ValueTask.FromResult(1);
        });
        feature.SetName("unchanged");
        RuntimeSnapshot<OperationTestState> previous = feature.Snapshot;
        AtomicValidationFeature.Rule = _ =>
        {
            if (reenter)
            {
                feature.SetName("nested");
                return true;
            }

            throw new InvalidOperationException("验证故障");
        };
        try
        {
            OperationResult<int> result = await feature.SubmitAsync();
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Faulted);
            await Assert.That(result.Exception is InvalidOperationException).IsTrue();
            await Assert.That(calls).IsEqualTo(0);
            await Assert.That(ReferenceEquals(feature.Snapshot, previous)).IsTrue();
            await Assert.That(feature.Snapshot.Version).IsEqualTo(previous.Version);
        }
        finally
        {
            AtomicValidationFeature.Rule = static _ => true;
        }

        feature.SetName("after fault");
        await Assert.That((await feature.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Completed);
    }

    /// <summary>验证取消发生在纯转换期间时最终提交点拒绝写回。</summary>
    /// <returns>表示提交点取消验证完成的任务。</returns>
    [Test]
    public async Task CancellationDuringReductionCannotCommitState()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource reducing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim release = new();
        OperationFeature feature = new(async operation =>
        {
            await operation.UpdateAsync((state, value) =>
            {
                reducing.SetResult();
                if (!release.Wait(Watchdog))
                {
                    throw new TimeoutException("转换测试未释放同步信号。");
                }

                return state with { Result = value };
            }, 99);
            return 99;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync(cancellation.Token);
        await reducing.Task.WaitAsync(Watchdog);
        cancellation.Cancel();
        release.Set();
        await Assert.That((await execution.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(0);
    }

    /// <summary>验证有效无变化反馈和同步服务代码仍由后台执行。</summary>
    /// <returns>表示后台执行与无变化提交验证完成的任务。</returns>
    [Test]
    public async Task SynchronousServiceRunsOutsideCallerAndNoChangeFeedbackIsValid()
    {
        using ManualResetEventSlim release = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        OperationFeature feature = new(async operation =>
        {
            entered.SetResult();
            if (!release.Wait(Watchdog))
            {
                throw new TimeoutException("服务测试未释放同步信号。");
            }

            await operation.UpdateAsync(static (state, _) => state, 0);
            return 1;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await entered.Task.WaitAsync(Watchdog);
        feature.SetName("editable");
        release.Set();
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo("editable");
    }

    /// <summary>验证不同操作和相同类型的不同实例各自独立执行。</summary>
    /// <returns>表示操作与实例隔离验证完成的任务。</returns>
    [Test]
    public async Task DifferentOperationsAndInstancesHaveIndependentAdmission()
    {
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        OperationFeature first = new(async _ =>
        {
            if (Interlocked.Increment(ref calls) == 2)
            {
                firstEntered.SetResult();
            }

            await release.Task;
            return 1;
        });
        OperationFeature second = new(async _ =>
        {
            secondEntered.SetResult();
            await release.Task;
            return 2;
        });
        first.SetName("one");
        second.SetName("two");
        Task<OperationResult<int>> submit = first.SubmitAsync();
        Task<OperationResult<int>> refresh = first.RefreshAsync();
        Task<OperationResult<int>> other = second.SubmitAsync();
        await Task.WhenAll(firstEntered.Task, secondEntered.Task).WaitAsync(Watchdog);
        await Assert.That(first.Snapshot.OperationStates.Values.All(static state => state.IsRunning)).IsTrue();
        await Assert.That(first.Snapshot.OperationStates.Count).IsEqualTo(2);
        await Assert.That(second.Snapshot.OperationStates.Count).IsEqualTo(1);
        first.SetName("edited one");
        await Assert.That(second.Snapshot.State.Name).IsEqualTo("two");
        release.SetResult();
        OperationResult<int>[] results = await Task.WhenAll(submit, refresh, other).WaitAsync(Watchdog);
        await Assert.That(results.All(static result => result.Kind == OperationResultKind.Completed)).IsTrue();
        await Assert.That(results.Select(static result => result.OperationId).Distinct().Count()).IsEqualTo(3);
    }

    /// <summary>验证同一子任务包含协作取消与非取消异常时不能掩盖故障。</summary>
    /// <returns>表示聚合子工作故障验证完成的任务。</returns>
    [Test]
    public async Task CooperativeExceptionInAggregateCannotHideChildFault()
    {
        using CancellationTokenSource cancellation = new();
        InvalidOperationException failure = new("第二项所属工作故障");
        OperationFeature feature = new(operation =>
        {
            cancellation.Cancel();
            operation.Track(Task.WhenAll(Task.FromException(new OperationCanceledException(cancellation.Token)), Task.FromException(failure)));
            return ValueTask.FromResult(1);
        });
        feature.SetName("ready");
        OperationResult<int> result = await feature.SubmitAsync(cancellation.Token);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Faulted);
        await Assert.That(ReferenceEquals(result.Exception, failure)).IsTrue();
    }

    /// <summary>验证业务方法返回后仍等待子工作及其注册的嵌套工作与状态提交。</summary>
    /// <returns>表示子工作完成屏障验证完成的任务。</returns>
    [Test]
    public async Task CompletionDrainsTrackedAndNestedWorkBeforeEndingContext()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Operation<OperationTestState>? captured = null;
        OperationFeature feature = new(operation =>
        {
            captured = operation;
            operation.Track(Child());
            return ValueTask.FromResult(10);

            async Task Child()
            {
                entered.SetResult();
                await releaseChild.Task;
                operation.Track(Nested());
            }

            async Task Nested()
            {
                nestedEntered.SetResult();
                await releaseNested.Task;
                await operation.UpdateAsync(static (state, value) => state with { Result = value }, 12);
            }
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await entered.Task.WaitAsync(Watchdog);
        await Assert.That(execution.IsCompleted).IsFalse();
        releaseChild.SetResult();
        await nestedEntered.Task.WaitAsync(Watchdog);
        await Assert.That(execution.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsTrue();
        releaseNested.SetResult();
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(12);
        Exception? updateFailure = null;
        Exception? trackFailure = null;
        RuntimeSnapshot<OperationTestState> completed = feature.Snapshot;
        try
        {
            await captured!.UpdateAsync(static (state, value) => state with { Result = value }, 99);
        }
        catch (Exception exception)
        {
            updateFailure = exception;
        }

        try
        {
            captured!.Track(Task.CompletedTask);
        }
        catch (Exception exception)
        {
            trackFailure = exception;
        }

        await Assert.That(updateFailure is InvalidOperationException).IsTrue();
        await Assert.That(trackFailure is InvalidOperationException).IsTrue();
        await Assert.That(ReferenceEquals(feature.Snapshot, completed)).IsTrue();
    }

    /// <summary>验证方法故障或取消仍等待子工作退出，子工作故障会改变完成结果。</summary>
    /// <param name="mode">业务故障、业务取消或子工作故障场景。</param>
    /// <returns>表示故障与子工作归属验证完成的任务。</returns>
    [Test]
    [Arguments("method-fault")]
    [Arguments("method-canceled")]
    [Arguments("child-fault")]
    public async Task FaultedOrCanceledMethodStillWaitsForTrackedWork(string mode)
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("所属工作故障");
        OperationFeature feature = new(operation =>
        {
            operation.Track(Child());
            if (mode == "method-fault")
            {
                throw failure;
            }

            if (mode == "method-canceled")
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }

            return ValueTask.FromResult(1);

            async Task Child()
            {
                entered.SetResult();
                await release.Task;
                operation.Track(Task.CompletedTask);
                if (mode == "child-fault")
                {
                    throw failure;
                }
            }
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync(cancellation.Token);
        await entered.Task.WaitAsync(Watchdog);
        await Assert.That(execution.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsTrue();
        release.SetResult();
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(mode == "method-canceled" ? OperationResultKind.Canceled : OperationResultKind.Faulted);
        if (mode != "method-canceled")
        {
            await Assert.That(ReferenceEquals(result.Exception, failure)).IsTrue();
        }
    }

    /// <summary>验证启动拒绝不执行服务，重复拒绝保留已有运行身份。</summary>
    /// <returns>表示启动准入验证完成的任务。</returns>
    [Test]
    public async Task ValidationAndDuplicateRejectionsAreVisibleWithoutClearingRunningOperation()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        OperationFeature feature = new(async _ =>
        {
            Interlocked.Increment(ref calls);
            entered.SetResult();
            await release.Task;
            return -1;
        });
        OperationResult<int> invalid = await feature.SubmitAsync();
        await Assert.That(invalid.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].Reason).IsEqualTo("ValidationFailed");
        await Assert.That(calls).IsEqualTo(0);
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await entered.Task.WaitAsync(Watchdog);
        Guid? runningId = feature.Snapshot.OperationStates["SubmitAsync"].RunningId;
        OperationResult<int> duplicate = await feature.SubmitAsync();
        await Assert.That(duplicate.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(duplicate.Reason).IsEqualTo("AlreadyRunning");
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].RunningId).IsEqualTo(runningId);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].LastAttemptId).IsEqualTo(duplicate.OperationId);
        await Assert.That(calls).IsEqualTo(1);
        release.SetResult();
        OperationResult<int> completed = await execution.WaitAsync(Watchdog);
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.HasValue).IsTrue();
        await Assert.That(completed.Value).IsEqualTo(-1);
    }

    /// <summary>验证非协作服务真实退出前不结束取消，既有提交保留且晚反馈失效。</summary>
    /// <returns>表示取消与完成边界验证完成的任务。</returns>
    [Test]
    public async Task CancellationWaitsForPhysicalExitAndRejectsCaughtLateFeedback()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? lateFailure = null;
        OperationFeature feature = new(async operation =>
        {
            await operation.UpdateAsync(static (state, value) => state with { Result = value }, 7);
            entered.SetResult();
            await release.Task;
            try
            {
                await operation.UpdateAsync(static (state, value) => state with { Result = value }, 99);
            }
            catch (Exception exception)
            {
                lateFailure = exception;
            }

            return 99;
        });
        feature.SetName("ready");
        Task<OperationResult<int>> execution = feature.SubmitAsync(cancellation.Token);
        await entered.Task.WaitAsync(Watchdog);
        cancellation.Cancel();
        await Assert.That(execution.IsCompleted).IsFalse();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsTrue();
        release.SetResult();
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Canceled);
        await Assert.That(result.HasValue).IsFalse();
        await Assert.That(lateFailure is OperationCanceledException).IsTrue();
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(7);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].LastResult).IsEqualTo(OperationResultKind.Canceled);
    }

    /// <summary>验证已取消的入口不执行服务。</summary>
    /// <returns>表示接纳前取消验证完成的任务。</returns>
    [Test]
    public async Task PreCanceledOperationDoesNotExecuteService()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        int calls = 0;
        OperationFeature feature = new(_ =>
        {
            calls++;
            return ValueTask.FromResult(1);
        });
        feature.SetName("ready");
        OperationResult<int> result = await feature.SubmitAsync(cancellation.Token);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Canceled);
        await Assert.That(calls).IsEqualTo(0);
    }

    /// <summary>验证服务异常和无关令牌的取消异常被结构化报告为故障。</summary>
    /// <param name="unrelatedCancellation">是否由无关令牌产生取消异常。</param>
    /// <returns>表示故障种类验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ServiceFaultAndUnrelatedCancellationAreFaulted(bool unrelatedCancellation)
    {
        using CancellationTokenSource unrelated = new();
        unrelated.Cancel();
        Exception failure = unrelatedCancellation ? new OperationCanceledException(unrelated.Token) : new InvalidOperationException("服务故障");
        OperationFeature feature = new(_ => ValueTask.FromException<int>(failure));
        feature.SetName("ready");
        OperationResult<int> result = await feature.SubmitAsync();
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Faulted);
        await Assert.That(ReferenceEquals(result.Exception, failure)).IsTrue();
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].Exception).IsEqualTo(failure);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsFalse();
    }

    /// <summary>验证业务捕获纯转换故障也不能把失败反馈伪装为正常完成。</summary>
    /// <param name="nullState">是否返回非法空状态。</param>
    /// <returns>表示纯规则故障原子性验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CaughtFeedbackRuleFailurePreservesBusinessStateAndFaultsOperation(bool nullState)
    {
        OperationFeature feature = new(async operation =>
        {
            try
            {
                await operation.UpdateAsync((state, _) => nullState ? null! : throw new InvalidOperationException("反馈故障"), 0);
            }
            catch (Exception)
            {
            }

            return 1;
        });
        feature.SetName("unchanged");
        OperationTestState previous = feature.Snapshot.State;
        OperationResult<int> result = await feature.SubmitAsync();
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Faulted);
        await Assert.That(result.Exception).IsNotNull();
        await Assert.That(result.HasValue).IsFalse();
        await Assert.That(ReferenceEquals(feature.Snapshot.State, previous)).IsTrue();
    }

    /// <summary>验证慢服务期间的输入可以提交且完成反馈保留并发编辑。</summary>
    /// <returns>表示操作闭环验证完成的任务。</returns>
    [Test]
    public async Task SlowOperationKeepsStartingInputAndPreservesConcurrentEdits()
    {
        TaskCompletionSource<string> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        OperationFeature feature = new(async operation =>
        {
            entered.SetResult(operation.Snapshot.Name);
            int value = await release.Task;
            await operation.UpdateAsync(static (state, result) => state with { Result = result }, value);
            return value;
        });
        feature.SetName("validated");
        Task<OperationResult<int>> execution = feature.SubmitAsync();
        await Assert.That(await entered.Task.WaitAsync(Watchdog)).IsEqualTo("validated");
        RuntimeSnapshot<OperationTestState> running = feature.Snapshot;
        await Assert.That(running.OperationStates["SubmitAsync"].IsRunning).IsTrue();
        feature.SetName("edited during IO");
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsTrue();
        release.SetResult(42);
        OperationResult<int> result = await execution.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(feature.Snapshot.State.Name).IsEqualTo("edited during IO");
        await Assert.That(feature.Snapshot.State.Result).IsEqualTo(42);
        await Assert.That(feature.Snapshot.OperationStates["SubmitAsync"].IsRunning).IsFalse();
        await Assert.That(running.State.Result).IsEqualTo(0);
    }
}

internal sealed record OperationTestState
{
    [Input]
    public string Name { get; init; } = string.Empty;

    public int Result { get; init; }
}

internal sealed partial class OperationFeature(Func<Operation<OperationTestState>, ValueTask<int>> service)
    : Feature<OperationTestState>(new())
{
    [Operation(Validate = nameof(CanSubmit))]
    private ValueTask<int> SubmitAsync(Operation<OperationTestState> operation) => service(operation);

    [Operation]
    private async Task<int> RefreshAsync(Operation<OperationTestState> operation) => await service(operation);

    private static bool CanSubmit(OperationTestState state) => state.Name.Length != 0;
}

internal sealed partial class AtomicValidationFeature(Func<Operation<OperationTestState>, ValueTask<int>> service)
    : Feature<OperationTestState>(new())
{
    internal static Func<OperationTestState, bool> Rule { get; set; } = static _ => true;

    [Operation(Validate = nameof(CanSubmit))]
    private ValueTask<int> SubmitAsync(Operation<OperationTestState> operation) => service(operation);

    private static bool CanSubmit(OperationTestState state) => Rule(state);
}
