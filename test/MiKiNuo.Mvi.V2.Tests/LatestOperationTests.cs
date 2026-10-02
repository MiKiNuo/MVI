using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>验证 Latest 操作通过公开入口隔离旧反馈与真实执行归属。</summary>
public sealed class LatestOperationTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>验证忽略取消的旧请求不能覆盖新结果或清除新运行身份。</summary>
    /// <returns>表示乱序搜索验证完成的任务。</returns>
    [Test]
    public async Task IgnoredCancellationCannotOverwriteLatestSearch()
    {
        TaskCompletionSource<Operation<LatestSearchState>> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LatestSearchFeature feature = new(async operation =>
        {
            bool first = operation.Snapshot.Query == "A";
            if (first)
            {
                firstEntered.SetResult(operation);
                await releaseFirst.Task;
            }
            else
            {
                secondEntered.SetResult();
                await releaseSecond.Task;
            }

            await operation.UpdateAsync(static (state, result) => state with { Result = result }, operation.Snapshot.Query);
            return operation.Snapshot.Query;
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await firstEntered.Task.WaitAsync(Watchdog);
        feature.SetQuery("B");
        Task<OperationResult<string>> second = feature.SearchAsync();
        try
        {
            await Assert.That(second.IsCompleted).IsFalse();
            await secondEntered.Task.WaitAsync(Watchdog);
            await Assert.That(old.CancellationToken.IsCancellationRequested).IsTrue();
            releaseSecond.SetResult();
            OperationResult<string> current = await second.WaitAsync(Watchdog);
            await Assert.That(current.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(current.Value).IsEqualTo("B");
            RuntimeSnapshot<LatestSearchState> completed = feature.Snapshot;
            releaseFirst.SetResult();
            OperationResult<string> superseded = await first.WaitAsync(Watchdog);
            await Assert.That(superseded.Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(superseded.HasValue).IsFalse();
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo("B");
            await Assert.That(ReferenceEquals(feature.Snapshot, completed)).IsTrue();
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseSecond.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(Watchdog);
        }
    }

    /// <summary>验证旧结果、进度、完成与异常在新执行期间及结束后都不能提交。</summary>
    /// <param name="feedback">旧执行产生的反馈种类。</param>
    /// <param name="latestFinished">是否先让新执行结束。</param>
    /// <returns>表示全部迟到反馈验证完成的任务。</returns>
    [Test]
    [Arguments("result", false)]
    [Arguments("progress", false)]
    [Arguments("finish", false)]
    [Arguments("fault", false)]
    [Arguments("result", true)]
    [Arguments("progress", true)]
    [Arguments("finish", true)]
    [Arguments("fault", true)]
    public async Task EveryOldFeedbackPreservesLatestSnapshot(string feedback, bool latestFinished)
    {
        TaskCompletionSource<Operation<LatestSearchState>> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException oldFault = new("旧服务仍可能发生真实异常");
        LatestSearchFeature feature = new(async operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                firstEntered.SetResult(operation);
                await releaseFirst.Task;
                if (feedback == "fault")
                {
                    throw oldFault;
                }

                return "A";
            }

            await operation.UpdateAsync(static (state, _) => state with { Result = "B", Progress = 60 }, 0);
            secondEntered.SetResult();
            await releaseSecond.Task;
            return "B";
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await firstEntered.Task.WaitAsync(Watchdog);
        feature.SetQuery("B");
        Task<OperationResult<string>> second = feature.SearchAsync();
        try
        {
            await secondEntered.Task.WaitAsync(Watchdog);
            if (latestFinished)
            {
                releaseSecond.SetResult();
                await Assert.That((await second.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            }

            RuntimeSnapshot<LatestSearchState> latest = feature.Snapshot;
            if (feedback is "result" or "progress")
            {
                Exception? failure = null;
                try
                {
                    await old.UpdateAsync((state, _) => feedback == "result"
                        ? state with { Result = "A" } : state with { Progress = 99 }, 0);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                await Assert.That(failure is OperationSupersededException).IsTrue();
            }

            releaseFirst.SetResult();
            OperationResult<string> superseded = await first.WaitAsync(Watchdog);
            await Assert.That(superseded.Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(ReferenceEquals(feature.Snapshot, latest)).IsTrue();
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo("B");
            await Assert.That(feature.Snapshot.State.Progress).IsEqualTo(60);
            await Assert.That(feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].IsRunning).IsEqualTo(!latestFinished);
            if (feedback == "fault")
            {
                await Assert.That(ReferenceEquals(superseded.Exception, oldFault)).IsTrue();
                await Assert.That(feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].Exception).IsNull();
            }
        }
        finally
        {
            releaseFirst.TrySetResult();
            releaseSecond.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(Watchdog);
        }
    }

    /// <summary>验证被拒绝、预先取消或验证故障的新尝试不会取代有效旧执行。</summary>
    /// <param name="attempt">新尝试的拒绝原因。</param>
    /// <returns>表示失败准入保持有效身份的验证任务。</returns>
    [Test]
    [Arguments("invalid")]
    [Arguments("canceled")]
    [Arguments("validation-fault")]
    public async Task UnadmittedAttemptCannotReplaceCurrentExecution(string attempt)
    {
        TaskCompletionSource<Operation<LatestSearchState>> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        LatestSearchFeature feature = new(async operation =>
        {
            Interlocked.Increment(ref calls);
            entered.SetResult(operation);
            await release.Task;
            await operation.UpdateAsync(static (state, _) => state with { Result = "A" }, 0);
            return "A";
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await entered.Task.WaitAsync(Watchdog);
        Guid? currentId = feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].RunningId;
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        if (attempt == "validation-fault")
        {
            LatestSearchFeature.Validation = static _ => throw new InvalidOperationException("启动验证故障");
        }

        try
        {
            feature.SetQuery(string.Empty);
            OperationResult<string> rejected = await feature.SearchAsync(attempt == "canceled" ? canceled.Token : default);
            await Assert.That(rejected.Kind).IsEqualTo(attempt == "canceled" ? OperationResultKind.Canceled
                : attempt == "validation-fault" ? OperationResultKind.Faulted : OperationResultKind.Rejected);
            await Assert.That(feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].RunningId).IsEqualTo(currentId);
            await Assert.That(old.CancellationToken.IsCancellationRequested).IsFalse();
            await Assert.That(calls).IsEqualTo(1);
            feature.SetNotes("edited during IO");
            release.SetResult();
            OperationResult<string> completed = await first.WaitAsync(Watchdog);
            await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo("A");
            await Assert.That(feature.Snapshot.State.Query).IsEqualTo(string.Empty);
            await Assert.That(feature.Snapshot.State.Notes).IsEqualTo("edited during IO");
        }
        finally
        {
            LatestSearchFeature.Validation = null;
            release.TrySetResult();
            await first.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证被取代后的反馈拒绝不会掩盖旧执行随后发生的真实故障。</summary>
    /// <returns>表示失效反馈和真实故障分别表达的验证任务。</returns>
    [Test]
    public async Task SupersededFeedbackDoesNotHideActualOldServiceFault()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException serviceFault = new("旧 IO 故障");
        LatestSearchFeature feature = new(async operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                entered.SetResult();
                await release.Task;
                try
                {
                    await operation.UpdateAsync(static (state, _) => state with { Result = "A" }, 0);
                }
                catch (OperationSupersededException)
                {
                }

                throw serviceFault;
            }

            return "B";
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> old = feature.SearchAsync();
        await entered.Task.WaitAsync(Watchdog);
        feature.SetQuery("B");
        await feature.SearchAsync();
        release.SetResult();
        OperationResult<string> result = await old.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Superseded);
        await Assert.That(ReferenceEquals(result.Exception, serviceFault)).IsTrue();
        await Assert.That(feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].Exception).IsNull();
    }

    /// <summary>验证被取代的执行仍保留子工作和嵌套工作的真实退出归属。</summary>
    /// <returns>表示被取代执行的完成屏障验证任务。</returns>
    [Test]
    public async Task SupersededExecutionStillDrainsTrackedAndNestedWork()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool feedbackRejected = false;
        LatestSearchFeature feature = new(operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                operation.Track(Child());
            }

            return ValueTask.FromResult(operation.Snapshot.Query);

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
                try
                {
                    await operation.UpdateAsync(static (state, _) => state with { Progress = 99 }, 0);
                }
                catch (OperationSupersededException)
                {
                    feedbackRejected = true;
                }
            }
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        await entered.Task.WaitAsync(Watchdog);
        feature.SetQuery("B");
        await feature.SearchAsync();
        RuntimeSnapshot<LatestSearchState> completed = feature.Snapshot;
        try
        {
            releaseChild.SetResult();
            await nestedEntered.Task.WaitAsync(Watchdog);
            await Assert.That(first.IsCompleted).IsFalse();
            releaseNested.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(feedbackRejected).IsTrue();
            await Assert.That(ReferenceEquals(feature.Snapshot, completed)).IsTrue();
        }
        finally
        {
            releaseChild.TrySetResult();
            releaseNested.TrySetResult();
            await first.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证取消回调在提交门外执行，可重入启动第三次搜索，故障归于旧执行。</summary>
    /// <param name="callbackFault">取消回调是否抛出真实异常。</param>
    /// <returns>表示取消回调隔离及资源释放验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CancellationCallbackCanReenterAndFaultWithoutCorruptingLatest(bool callbackFault)
    {
        TaskCompletionSource<Operation<LatestSearchState>> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource callbackFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource thirdEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OperationResult<string>>? third = null;
        LatestSearchFeature? feature = null;
        feature = new LatestSearchFeature(async operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                using CancellationTokenRegistration registration = operation.CancellationToken.Register(() =>
                {
                    Task.Run(() => feature!.SetNotes("input from cancellation callback")).WaitAsync(Watchdog).GetAwaiter().GetResult();
                    feature!.SetQuery("C");
                    third = feature.SearchAsync();
                    callbackFinished.SetResult();
                    if (callbackFault)
                    {
                        throw new InvalidOperationException("取消回调故障");
                    }
                });
                firstEntered.SetResult(operation);
                await release.Task;
                return "A";
            }

            if (operation.Snapshot.Query == "C")
            {
                await operation.UpdateAsync(static (state, _) => state with { Result = "C", Progress = 80 }, 0);
                thirdEntered.SetResult();
            }

            await release.Task;
            return operation.Snapshot.Query;
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await firstEntered.Task.WaitAsync(Watchdog);
        WaitHandle oldHandle = old.CancellationToken.WaitHandle;
        feature.SetQuery("B");
        Task<OperationResult<string>> second = feature.SearchAsync();
        try
        {
            await Task.WhenAll(callbackFinished.Task, thirdEntered.Task).WaitAsync(Watchdog);
            RuntimeSnapshot<LatestSearchState> latest = feature.Snapshot;
            await Assert.That(latest.State.Notes).IsEqualTo("input from cancellation callback");
            await Assert.That(latest.State.Result).IsEqualTo("C");
            await Assert.That(latest.OperationStates[nameof(feature.SearchAsync)].IsRunning).IsTrue();
            await Assert.That(oldHandle.SafeWaitHandle.IsClosed).IsFalse();
            release.SetResult();
            OperationResult<string>[] results = await Task.WhenAll(first, second, third!).WaitAsync(Watchdog);
            await Assert.That(results[0].Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(results[0].Exception is not null).IsEqualTo(callbackFault);
            await Assert.That(results[1].Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(results[2].Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo("C");
            await Assert.That(feature.Snapshot.OperationStates[nameof(feature.SearchAsync)].LastAttemptId).IsEqualTo(results[2].OperationId);
            await Assert.That(oldHandle.SafeWaitHandle.IsClosed).IsTrue();
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(Watchdog);
            if (third is not null)
            {
                await third.WaitAsync(Watchdog);
            }
        }
    }

    /// <summary>验证旧业务结束后仍等待取消回调，回调能登记其使用实例资源的工作。</summary>
    /// <returns>表示取消回调的真实退出屏障验证任务。</returns>
    [Test]
    public async Task OldExecutionWaitsForCancellationCallbacksAndTheirTrackedWork()
    {
        TaskCompletionSource<Operation<LatestSearchState>> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource methodExited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseMethod = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseCallback = new();
        LatestSearchFeature feature = new(async operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                // 不在业务方法退出时 Dispose：该回调仍是本次取消请求的所属工作。
                operation.CancellationToken.Register(() =>
                {
                    callbackEntered.SetResult();
                    if (!releaseCallback.Wait(Watchdog))
                    {
                        throw new TimeoutException("取消回调未释放。");
                    }

                    operation.Track(releaseChild.Task);
                });
                entered.SetResult(operation);
                await releaseMethod.Task;
                methodExited.SetResult();
            }

            return operation.Snapshot.Query;
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await entered.Task.WaitAsync(Watchdog);
        WaitHandle handle = old.CancellationToken.WaitHandle;
        feature.SetQuery("B");
        await feature.SearchAsync();
        try
        {
            await callbackEntered.Task.WaitAsync(Watchdog);
            releaseMethod.SetResult();
            await methodExited.Task.WaitAsync(Watchdog);
            await Assert.That(first.IsCompleted).IsFalse();
            await Assert.That(handle.SafeWaitHandle.IsClosed).IsFalse();
            releaseCallback.Set();
            await Assert.That(first.IsCompleted).IsFalse();
            releaseChild.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(handle.SafeWaitHandle.IsClosed).IsTrue();
        }
        finally
        {
            releaseCallback.Set();
            releaseMethod.TrySetResult();
            releaseChild.TrySetResult();
            await first.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证 Latest 的验证、身份切换与输入采样不会混用竞争编辑。</summary>
    /// <returns>表示 Latest 原子准入验证任务。</returns>
    [Test]
    public async Task LatestValidationAndInputSamplingAreAtomic()
    {
        TaskCompletionSource<Operation<LatestSearchState>> firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> secondInput = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource validationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource inputAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseService = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseValidation = new();
        LatestSearchFeature feature = new(async operation =>
        {
            if (operation.Snapshot.Query == "A")
            {
                firstEntered.SetResult(operation);
            }
            else
            {
                secondInput.SetResult(operation.Snapshot.Query);
            }

            await releaseService.Task;
            await operation.UpdateAsync(static (state, result) => state with { Result = result }, operation.Snapshot.Query);
            return operation.Snapshot.Query;
        });
        feature.SetQuery("A");
        Task<OperationResult<string>> first = feature.SearchAsync();
        Operation<LatestSearchState> old = await firstEntered.Task.WaitAsync(Watchdog);
        feature.SetQuery("validated B");
        LatestSearchFeature.Validation = state =>
        {
            validationEntered.SetResult();
            if (!releaseValidation.Wait(Watchdog))
            {
                throw new TimeoutException("启动验证未释放。");
            }

            return state.Query == "validated B";
        };
        Task<OperationResult<string>> second = Task.Run(() => feature.SearchAsync());
        Task? input = null;
        try
        {
            await validationEntered.Task.WaitAsync(Watchdog);
            input = Task.Run(() =>
            {
                inputAttempted.SetResult();
                feature.SetQuery(string.Empty);
                feature.SetNotes("new note");
            });
            await inputAttempted.Task.WaitAsync(Watchdog);
            releaseValidation.Set();
            await input.WaitAsync(Watchdog);
            await Assert.That(await secondInput.Task.WaitAsync(Watchdog)).IsEqualTo("validated B");
            await Assert.That(old.CancellationToken.IsCancellationRequested).IsTrue();
            releaseService.SetResult();
            await Assert.That((await second.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(feature.Snapshot.State.Result).IsEqualTo("validated B");
            await Assert.That(feature.Snapshot.State.Query).IsEqualTo(string.Empty);
            await Assert.That(feature.Snapshot.State.Notes).IsEqualTo("new note");
        }
        finally
        {
            releaseValidation.Set();
            releaseService.TrySetResult();
            LatestSearchFeature.Validation = null;
            await Task.WhenAll(first, second).WaitAsync(Watchdog);
            if (input is not null)
            {
                await input.WaitAsync(Watchdog);
            }
        }
    }

    /// <summary>验证同类型独立实例与不同操作不因 Latest 切换而失效。</summary>
    /// <returns>表示按实例按操作隔离验证任务。</returns>
    [Test]
    public async Task LatestReplacementDoesNotInvalidateOtherOperationOrInstance()
    {
        TaskCompletionSource<Operation<LatestSearchState>> oldEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Operation<LatestSearchState>> refreshEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Operation<LatestSearchState>> otherEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        LatestSearchFeature first = new(async operation =>
        {
            int call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                oldEntered.SetResult(operation);
            }
            else if (call == 2)
            {
                refreshEntered.SetResult(operation);
            }

            await release.Task;
            return operation.Snapshot.Query;
        });
        LatestSearchFeature other = new(async operation =>
        {
            otherEntered.SetResult(operation);
            await release.Task;
            return operation.Snapshot.Query;
        });
        first.SetQuery("A");
        other.SetQuery("other");
        Task<OperationResult<string>> old = first.SearchAsync();
        Operation<LatestSearchState> oldContext = await oldEntered.Task.WaitAsync(Watchdog);
        Task<OperationResult<string>> refresh = first.RefreshAsync();
        Operation<LatestSearchState> refreshContext = await refreshEntered.Task.WaitAsync(Watchdog);
        Task<OperationResult<string>> independent = other.SearchAsync();
        Operation<LatestSearchState> otherContext = await otherEntered.Task.WaitAsync(Watchdog);
        first.SetQuery("B");
        Task<OperationResult<string>> latest = first.SearchAsync();
        try
        {
            await Assert.That(oldContext.CancellationToken.IsCancellationRequested).IsTrue();
            await Assert.That(refreshContext.CancellationToken.IsCancellationRequested).IsFalse();
            await Assert.That(otherContext.CancellationToken.IsCancellationRequested).IsFalse();
            await refreshContext.UpdateAsync(static (state, _) => state with { Progress = 30 }, 0);
            await otherContext.UpdateAsync(static (state, _) => state with { Result = "independent" }, 0);
            release.SetResult();
            OperationResult<string>[] results = await Task.WhenAll(old, refresh, independent, latest).WaitAsync(Watchdog);
            await Assert.That(results[0].Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(results.Skip(1).All(static result => result.Kind == OperationResultKind.Completed)).IsTrue();
            await Assert.That(first.Snapshot.State.Progress).IsEqualTo(30);
            await Assert.That(other.Snapshot.State.Result).IsEqualTo("independent");
            await Assert.That(other.Snapshot.State.Query).IsEqualTo("other");
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(old, refresh, independent, latest).WaitAsync(Watchdog);
        }
    }
}

internal sealed record LatestSearchState
{
    [Input]
    public string Query { get; init; } = string.Empty;

    [Input]
    public string Notes { get; init; } = string.Empty;

    public string Result { get; init; } = string.Empty;

    public int Progress { get; init; }
}

internal sealed partial class LatestSearchFeature(Func<Operation<LatestSearchState>, ValueTask<string>> service)
    : Feature<LatestSearchState>(new())
{
    internal static Func<LatestSearchState, bool>? Validation { get; set; }

    [Operation(Validate = nameof(CanSearch), Concurrency = OperationConcurrency.Latest)]
    private ValueTask<string> SearchAsync(Operation<LatestSearchState> operation) => service(operation);

    [Operation]
    private ValueTask<string> RefreshAsync(Operation<LatestSearchState> operation) => service(operation);

    private static bool CanSearch(LatestSearchState state) => Validation?.Invoke(state) ?? state.Query.Length != 0;
}
