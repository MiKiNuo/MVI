using System.Collections.Concurrent;
using System.Collections.Immutable;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开契约验证独立功能实例的定向请求。</summary>
public sealed class MediatorRequestTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>验证唯一目标请求返回时目标的业务与操作状态已经提交。</summary>
    /// <returns>表示请求闭环验证完成的任务。</returns>
    [Test]
    public async Task UniqueTargetReturnsAfterItsOwnStateAndOperationCommit()
    {
        Mediator mediator = new();
        RequestDetailsFeature details = new();
        using IDisposable registration = mediator.Register(details.Load);

        RequestResult<string> result = await mediator.SendAsync<LoadDetails, string>(new(7)).WaitAsync(Watchdog);

        await Assert.That(result.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(result.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(result.OperationResult.Value).IsEqualTo("details 7");
        await Assert.That(details.Snapshot.State.Text).IsEqualTo("details 7");
        await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsFalse();
        await Assert.That(details.Snapshot.OperationStates["Load"].LastResult).IsEqualTo(OperationResultKind.Completed);
    }

    /// <summary>验证未接线的契约和错误的返回值契约明确报告缺失。</summary>
    /// <returns>表示契约缺失验证完成的任务。</returns>
    [Test]
    public async Task MissingAndWrongResponseContractDoNotStartTarget()
    {
        Mediator mediator = new();
        RequestResult<string> missing = await mediator.SendAsync<LoadDetails, string>(new(7));
        await Assert.That(missing.Kind).IsEqualTo(RequestResultKind.MissingTarget);
        await Assert.That(missing.OperationResult).IsNull();

        RequestDetailsFeature details = new();
        using IDisposable registration = mediator.Register(details.Load);
        RequestResult<int> wrongResponse = await mediator.SendAsync<LoadDetails, int>(new(7));
        await Assert.That(wrongResponse.Kind).IsEqualTo(RequestResultKind.MissingTarget);
        await Assert.That(details.Snapshot.Version).IsEqualTo(0);
    }

    /// <summary>验证同类型多目标不隐式选择，明确端口只影响该实例。</summary>
    /// <returns>表示明确目标验证完成的任务。</returns>
    [Test]
    public async Task AmbiguousContractRequiresExplicitTarget()
    {
        Mediator mediator = new();
        RequestDetailsFeature first = new();
        RequestDetailsFeature second = new();
        using IDisposable firstRegistration = mediator.Register(first.Load);
        using IDisposable secondRegistration = mediator.Register(second.Load);

        RequestResult<string> ambiguous = await mediator.SendAsync<LoadDetails, string>(new(7));
        await Assert.That(ambiguous.Kind).IsEqualTo(RequestResultKind.AmbiguousTarget);
        await Assert.That(ambiguous.OperationResult).IsNull();
        await Assert.That(first.Snapshot.Version).IsEqualTo(0);
        await Assert.That(second.Snapshot.Version).IsEqualTo(0);

        RequestResult<string> explicitResult = await mediator.SendAsync(new LoadDetails(7), second.Load).WaitAsync(Watchdog);
        await Assert.That(explicitResult.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(explicitResult.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(first.Snapshot.Version).IsEqualTo(0);
        await Assert.That(second.Snapshot.State.ObjectId).IsEqualTo(7);
    }

    /// <summary>验证重复端口接线幂等，旧回执不能移除重新建立的路由。</summary>
    /// <returns>表示动态路由清理验证完成的任务。</returns>
    [Test]
    public async Task DuplicateRegistrationAndHostRemovalAreDeterministic()
    {
        Mediator mediator = new();
        RequestDetailsFeature details = new();
        IDisposable original = mediator.Register(details.Load);
        using IDisposable duplicate = mediator.Register(details.Load);
        await Assert.That(ReferenceEquals(original, duplicate)).IsTrue();
        RequestResult<string> unique = await mediator.SendAsync<LoadDetails, string>(new(7)).WaitAsync(Watchdog);
        await Assert.That(unique.Kind).IsEqualTo(RequestResultKind.Responded);

        original.Dispose();
        RequestResult<string> removed = await mediator.SendAsync(new LoadDetails(7), details.Load);
        RequestResult<string> missing = await mediator.SendAsync<LoadDetails, string>(new(7));
        await Assert.That(removed.Kind).IsEqualTo(RequestResultKind.TargetUnavailable);
        await Assert.That(missing.Kind).IsEqualTo(RequestResultKind.MissingTarget);

        using IDisposable replacement = mediator.Register(details.Load);
        original.Dispose();
        RequestResult<string> restored = await mediator.SendAsync<LoadDetails, string>(new(8)).WaitAsync(Watchdog);
        await Assert.That(restored.OperationResult!.Value).IsEqualTo("details 8");
    }

    /// <summary>验证独立范围默认隔离，宿主可显式将同一端口接入另一个范围。</summary>
    /// <returns>表示范围隔离与显式接线验证完成的任务。</returns>
    [Test]
    public async Task SeparateScopesRequireExplicitHostWiring()
    {
        Mediator firstScope = new();
        Mediator secondScope = new();
        RequestDetailsFeature first = new(static (_, _) => ValueTask.FromResult("first scope"));
        RequestDetailsFeature second = new(static (_, _) => ValueTask.FromResult("second scope"));
        using IDisposable firstRegistration = firstScope.Register(first.Load);
        using IDisposable secondRegistration = secondScope.Register(second.Load);
        RequestResult<string> outside = await firstScope.SendAsync(new LoadDetails(7), second.Load);
        await Assert.That(outside.Kind).IsEqualTo(RequestResultKind.TargetUnavailable);

        RequestResult<string> local = await firstScope.SendAsync<LoadDetails, string>(new(7)).WaitAsync(Watchdog);
        await Assert.That(local.OperationResult!.Value).IsEqualTo("first scope");
        await Assert.That(second.Snapshot.Version).IsEqualTo(0);

        using IDisposable exported = firstScope.Register(second.Load);
        RequestResult<string> wired = await firstScope.SendAsync(new LoadDetails(7), second.Load).WaitAsync(Watchdog);
        await Assert.That(wired.OperationResult!.Value).IsEqualTo("second scope");
        exported.Dispose();
        RequestResult<string> stillLocal = await secondScope.SendAsync<LoadDetails, string>(new(8)).WaitAsync(Watchdog);
        await Assert.That(stillLocal.OperationResult!.Value).IsEqualTo("second scope");
    }

    /// <summary>验证程序调用和请求入口使用同一业务验证，无效输入不能调用服务。</summary>
    /// <param name="disabled">是否由实例业务状态禁止执行。</param>
    /// <returns>表示统一验证入口验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task InvalidBusinessInputUsesSameValidationAsDirectCall(bool disabled)
    {
        int serviceCalls = 0;
        RequestDetailsFeature details = new((_, _) =>
        {
            Interlocked.Increment(ref serviceCalls);
            return ValueTask.FromResult("should not execute");
        });
        details.SetEnabled(!disabled);
        LoadDetails request = new(disabled ? 7 : 0);
        OperationResult<string> direct = await details.LoadAsync(request);
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        RequestResult<string> sent = await mediator.SendAsync(request, details.Load);

        await Assert.That(direct.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(sent.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(sent.OperationResult!.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(sent.OperationResult.Reason).IsEqualTo("ValidationFailed");
        await Assert.That(details.Snapshot.OperationStates["Load"].Reason).IsEqualTo("ValidationFailed");
        await Assert.That(serviceCalls).IsEqualTo(0);
    }

    /// <summary>验证验证故障和服务故障保留目标结构化结果，且验证故障不提交状态。</summary>
    /// <param name="validationFault">是否在启动纯规则中发生故障。</param>
    /// <returns>表示目标故障验证完成的任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TargetFaultsRemainOriginalOperationResults(bool validationFault)
    {
        InvalidOperationException failure = new("target failure");
        int serviceCalls = 0;
        RequestDetailsFeature details = new((_, _) =>
        {
            serviceCalls++;
            return ValueTask.FromException<string>(failure);
        }, (_, _) => validationFault ? throw failure : true);
        RuntimeSnapshot<RequestDetailsState> before = details.Snapshot;
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);

        RequestResult<string> sent = await mediator.SendAsync(new LoadDetails(7), details.Load).WaitAsync(Watchdog);

        await Assert.That(sent.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(sent.OperationResult!.Kind).IsEqualTo(OperationResultKind.Faulted);
        await Assert.That(ReferenceEquals(sent.OperationResult.Exception, failure)).IsTrue();
        await Assert.That(sent.OperationResult.HasValue).IsFalse();
        await Assert.That(serviceCalls).IsEqualTo(validationFault ? 0 : 1);
        if (validationFault)
        {
            await Assert.That(ReferenceEquals(details.Snapshot, before)).IsTrue();
            await Assert.That(sent.OperationResult.Reason).IsEqualTo("ValidationFault");
        }
        else
        {
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsFalse();
            await Assert.That(details.Snapshot.OperationStates["Load"].Exception).IsEqualTo(failure);
        }
    }

    /// <summary>验证业务失败载荷仍属于目标正常完成，而不是路由失败。</summary>
    /// <returns>表示业务结果与执行结果分离验证完成的任务。</returns>
    [Test]
    public async Task BusinessFailureValueIsStillCompleted()
    {
        RequestBusinessFailureFeature details = new();
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Lookup);
        RequestResult<LookupReply> sent = await mediator.SendAsync(new LoadDetails(7), details.Lookup).WaitAsync(Watchdog);
        await Assert.That(sent.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(sent.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(sent.OperationResult.HasValue).IsTrue();
        await Assert.That(sent.OperationResult.Value!.Found).IsFalse();
        await Assert.That(details.Snapshot.State.Text).IsEqualTo("not found");
    }

    /// <summary>验证同类型同业务对象多开时状态、运行身份和请求路由均独立。</summary>
    /// <returns>表示多实例在途隔离验证完成的任务。</returns>
    [Test]
    public async Task SameTypeAndSameBusinessObjectHaveIsolatedInFlightOperations()
    {
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> firstRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> secondRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature first = new(async (_, _) =>
        {
            firstEntered.SetResult();
            return await firstRelease.Task;
        });
        RequestDetailsFeature second = new(async (_, _) =>
        {
            secondEntered.SetResult();
            return await secondRelease.Task;
        });
        first.SetDraft("first draft");
        second.SetDraft("second draft");
        Mediator mediator = new();
        using IDisposable firstRegistration = mediator.Register(first.Load);
        using IDisposable secondRegistration = mediator.Register(second.Load);
        Task<RequestResult<string>> firstRequest = mediator.SendAsync(new LoadDetails(7), first.Load);
        Task<RequestResult<string>> secondRequest = mediator.SendAsync(new LoadDetails(7), second.Load);
        try
        {
            await Task.WhenAll(firstEntered.Task, secondEntered.Task).WaitAsync(Watchdog);
            Guid? firstId = first.Snapshot.OperationStates["Load"].RunningId;
            Guid? secondId = second.Snapshot.OperationStates["Load"].RunningId;
            await Assert.That(firstId.HasValue && secondId.HasValue && firstId != secondId).IsTrue();
            await Assert.That(ReferenceEquals(first.Load, second.Load)).IsFalse();

            first.SetEnabled(false);
            RequestResult<string> repeat = await mediator.SendAsync(new LoadDetails(0), first.Load);
            await Assert.That(repeat.OperationResult!.Kind).IsEqualTo(OperationResultKind.Rejected);
            await Assert.That(repeat.OperationResult.Reason).IsEqualTo("AlreadyRunning");
            await Assert.That(first.Snapshot.OperationStates["Load"].RunningId).IsEqualTo(firstId);
            await Assert.That(second.Snapshot.OperationStates["Load"].RunningId).IsEqualTo(secondId);
            first.SetDraft("edited first during IO");
            firstRelease.SetResult("first result");
            RequestResult<string> firstCompleted = await firstRequest.WaitAsync(Watchdog);
            await Assert.That(firstCompleted.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(first.Snapshot.State.Text).IsEqualTo("first result");
            await Assert.That(first.Snapshot.State.ObjectId).IsEqualTo(7);
            await Assert.That(first.Snapshot.State.Draft).IsEqualTo("edited first during IO");
            await Assert.That(second.Snapshot.State.Text).IsEqualTo(string.Empty);
            await Assert.That(second.Snapshot.State.Draft).IsEqualTo("second draft");
            await Assert.That(second.Snapshot.OperationStates["Load"].RunningId).IsEqualTo(secondId);
            await Assert.That(secondRequest.IsCompleted).IsFalse();

            secondRelease.SetResult("second result");
            RequestResult<string> secondCompleted = await secondRequest.WaitAsync(Watchdog);
            await Assert.That(secondCompleted.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(second.Snapshot.State.Text).IsEqualTo("second result");
            await Assert.That(second.Snapshot.State.ObjectId).IsEqualTo(7);
            await Assert.That(first.Snapshot.State.Text).IsEqualTo("first result");
        }
        finally
        {
            firstRelease.TrySetResult("first result");
            secondRelease.TrySetResult("second result");
            await Task.WhenAll(firstRequest, secondRequest).WaitAsync(Watchdog);
        }
    }

    /// <summary>验证请求完成等待业务返回后登记的嵌套工作及其状态提交。</summary>
    /// <returns>表示真实目标完成屏障验证完成的任务。</returns>
    [Test]
    public async Task SendWaitsForTrackedNestedWorkAndFinalFeedback()
    {
        TaskCompletionSource workEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseWork = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature details = new((operation, _) =>
        {
            operation.Track(TrackNestedAsync(operation));
            return ValueTask.FromResult("method returned");
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        Task<RequestResult<string>> request = mediator.SendAsync(new LoadDetails(7), details.Load);
        try
        {
            await workEntered.Task.WaitAsync(Watchdog);
            await Assert.That(request.IsCompleted).IsFalse();
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsTrue();
            releaseWork.SetResult();
            await nestedEntered.Task.WaitAsync(Watchdog);
            await Assert.That(request.IsCompleted).IsFalse();
            releaseNested.SetResult();
            RequestResult<string> result = await request.WaitAsync(Watchdog);
            await Assert.That(result.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(result.OperationResult.Value).IsEqualTo("method returned");
            await Assert.That(details.Snapshot.State.TrackedUpdates).IsEqualTo(42);
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsFalse();
        }
        finally
        {
            releaseWork.TrySetResult();
            releaseNested.TrySetResult();
            await request.WaitAsync(Watchdog);
        }

        async Task TrackNestedAsync(Operation<RequestDetailsState> operation)
        {
            workEntered.SetResult();
            await releaseWork.Task;
            operation.Track(UpdateNestedAsync(operation));
        }

        async Task UpdateNestedAsync(Operation<RequestDetailsState> operation)
        {
            nestedEntered.SetResult();
            await releaseNested.Task;
            await operation.UpdateAsync(static (state, value) => state with { TrackedUpdates = value }, 42);
        }
    }

    /// <summary>验证关闭端口只阻止新请求，已接纳请求仍能提交目标反馈并完成。</summary>
    /// <returns>表示端口停止接纳验证完成的任务。</returns>
    [Test]
    public async Task DeactivationRejectsNewAdmissionAndPreservesAcceptedWork()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature details = new(async (_, _) =>
        {
            entered.SetResult();
            return await release.Task;
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        Task<RequestResult<string>> accepted = mediator.SendAsync(new LoadDetails(7), details.Load);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            Guid? running = details.Snapshot.OperationStates["Load"].RunningId;
            details.Load.Deactivate();
            RequestResult<string> explicitUnavailable = await mediator.SendAsync(new LoadDetails(7), details.Load);
            RequestResult<string> autoUnavailable = await mediator.SendAsync<LoadDetails, string>(new(7));
            await Assert.That(explicitUnavailable.Kind).IsEqualTo(RequestResultKind.TargetUnavailable);
            await Assert.That(autoUnavailable.Kind).IsEqualTo(RequestResultKind.TargetUnavailable);
            await Assert.That(details.Snapshot.OperationStates["Load"].RunningId).IsEqualTo(running);
            release.SetResult("accepted work committed");
            RequestResult<string> completed = await accepted.WaitAsync(Watchdog);
            await Assert.That(completed.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(details.Snapshot.State.Text).IsEqualTo("accepted work committed");
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsFalse();
        }
        finally
        {
            release.TrySetResult("cleanup");
            await accepted.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证接纳前取消不会启动目标或改变其快照。</summary>
    /// <returns>表示接纳前取消验证完成的任务。</returns>
    [Test]
    public async Task PreCanceledRequestDoesNotStartTarget()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        RequestDetailsFeature details = new();
        RuntimeSnapshot<RequestDetailsState> before = details.Snapshot;
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        RequestResult<string> result = await mediator.SendAsync(new LoadDetails(7), details.Load, cancellation.Token);
        await Assert.That(result.Kind).IsEqualTo(RequestResultKind.WaitCanceled);
        await Assert.That(result.OperationResult).IsNull();
        await Assert.That(ReferenceEquals(details.Snapshot, before)).IsTrue();
    }

    /// <summary>验证取消调用方等待后目标仍拥有执行，并通过受跟踪工作完成状态提交。</summary>
    /// <returns>表示等待取消与目标执行归属验证完成的任务。</returns>
    [Test]
    public async Task WaitCancellationDoesNotCancelAcceptedTargetExecution()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource committed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool targetTokenCanCancel = true;
        RequestDetailsFeature details = new(async (operation, _) =>
        {
            targetTokenCanCancel = operation.CancellationToken.CanBeCanceled;
            entered.SetResult();
            await release.Task;
            await operation.UpdateAsync(static (state, value) => state with { Text = value }, "target continued");
            committed.SetResult();
            return "target continued";
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        Task<RequestResult<string>> waiting = mediator.SendAsync(new LoadDetails(7), details.Load, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            cancellation.Cancel();
            RequestResult<string> canceled = await waiting.WaitAsync(Watchdog);
            await Assert.That(canceled.Kind).IsEqualTo(RequestResultKind.WaitCanceled);
            await Assert.That(canceled.OperationResult).IsNull();
            await Assert.That(targetTokenCanCancel).IsFalse();
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsTrue();
            release.SetResult();
            await committed.Task.WaitAsync(Watchdog);
            await Assert.That(details.Snapshot.State.Text).IsEqualTo("target continued");
        }
        finally
        {
            release.TrySetResult();
            await committed.Task.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证请求原子采样输入，纯规则期间不持有范围与端口锁，接纳后停用不撤销请求。</summary>
    /// <returns>表示原子请求启动与范围独立性验证完成的任务。</returns>
    [Test]
    public async Task RequestValidationSamplesAtomicInputWithoutHoldingScopeRegistryLock()
    {
        TaskCompletionSource validationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource inputAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> sampled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseValidation = new();
        RequestDetailsFeature details = new((operation, request) =>
        {
            sampled.SetResult($"{operation.Snapshot.Draft}/{request.ObjectId}");
            return ValueTask.FromResult("finished");
        }, (state, request) =>
        {
            validationEntered.SetResult();
            if (!releaseValidation.Wait(Watchdog))
            {
                throw new TimeoutException("请求验证屏障未释放。");
            }

            return state.Draft == "validated" && request.ObjectId == 7;
        });
        details.SetDraft("validated");
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        Task<RequestResult<string>> request = Task.Run(() => mediator.SendAsync(new LoadDetails(7), details.Load));
        Task? edit = null;
        try
        {
            await validationEntered.Task.WaitAsync(Watchdog);
            RequestDetailsFeature unrelated = new();
            using IDisposable unrelatedRegistration = await Task.Run(() => mediator.Register(unrelated.Load)).WaitAsync(Watchdog);
            await Task.Run(details.Load.Deactivate).WaitAsync(Watchdog);
            edit = Task.Run(() =>
            {
                inputAttempted.SetResult();
                details.SetDraft("new edit");
            });
            await inputAttempted.Task.WaitAsync(Watchdog);
            releaseValidation.Set();
            await edit.WaitAsync(Watchdog);
            await Assert.That(await sampled.Task.WaitAsync(Watchdog)).IsEqualTo("validated/7");
            RequestResult<string> completed = await request.WaitAsync(Watchdog);
            await Assert.That(completed.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(details.Snapshot.State.Draft).IsEqualTo("new edit");
        }
        finally
        {
            releaseValidation.Set();
            if (edit is not null)
            {
                await edit.WaitAsync(Watchdog);
            }

            await request.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证发送者等待目标响应后通过自己的操作反馈入口更新自身状态。</summary>
    /// <returns>表示发送者状态闭环验证完成的任务。</returns>
    [Test]
    public async Task SenderAppliesResponseThroughItsOwnOperationFeedback()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature details = new(async (_, _) =>
        {
            entered.SetResult();
            return await release.Task;
        });
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(details.Load);
        RequestQueryFeature sender = new(mediator, details.Load);
        sender.SetObjectId(7);
        Task<OperationResult<ImmutableArray<RequestResult<string>>>> query = sender.QueryAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            await Assert.That(sender.Snapshot.State.Responses.IsEmpty).IsTrue();
            await Assert.That(sender.Snapshot.OperationStates["QueryAsync"].IsRunning).IsTrue();
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsTrue();
            release.SetResult("target answer");
            OperationResult<ImmutableArray<RequestResult<string>>> result = await query.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(result.Value[0].OperationResult!.Value).IsEqualTo("target answer");
            await Assert.That(sender.Snapshot.State.Responses[0]).IsEqualTo("target answer");
            await Assert.That(sender.Snapshot.OperationStates["QueryAsync"].IsRunning).IsFalse();
            await Assert.That(details.Snapshot.State.Text).IsEqualTo("target answer");
            await Assert.That(details.Snapshot.OperationStates["Load"].IsRunning).IsFalse();
        }
        finally
        {
            release.TrySetResult("cleanup");
            await query.WaitAsync(Watchdog);
        }
    }

    /// <summary>验证多目标业务由协调功能明确分派，而不是向范围隐式广播。</summary>
    /// <returns>表示明确多目标协调验证完成的任务。</returns>
    [Test]
    public async Task CoordinatorExplicitlyRequestsEachChosenPort()
    {
        RequestDetailsFeature first = new(static (_, _) => ValueTask.FromResult("first answer"));
        RequestDetailsFeature second = new(static (_, _) => ValueTask.FromResult("second answer"));
        RequestDetailsFeature excluded = new(static (_, _) => ValueTask.FromResult("not chosen"));
        Mediator mediator = new();
        using IDisposable firstRegistration = mediator.Register(first.Load);
        using IDisposable secondRegistration = mediator.Register(second.Load);
        using IDisposable excludedRegistration = mediator.Register(excluded.Load);
        RequestQueryFeature sender = new(mediator, first.Load, second.Load);
        sender.SetObjectId(7);
        OperationResult<ImmutableArray<RequestResult<string>>> result = await sender.QueryAsync().WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(result.Value.Length).IsEqualTo(2);
        await Assert.That(sender.Snapshot.State.Responses[0]).IsEqualTo("first answer");
        await Assert.That(sender.Snapshot.State.Responses[1]).IsEqualTo("second answer");
        await Assert.That(first.Snapshot.State.ObjectId).IsEqualTo(7);
        await Assert.That(second.Snapshot.State.ObjectId).IsEqualTo(7);
        await Assert.That(excluded.Snapshot.Version).IsEqualTo(0);
    }

    /// <summary>验证两个内联 View 回调可互相请求另一个端口，不形成接纳锁等待环。</summary>
    /// <returns>表示跨端口内联展示回调验证完成的任务。</returns>
    [Test]
    public async Task InlineProjectionCallbacksCanRequestEachOthersPort()
    {
        TaskCompletionSource firstDisplayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondDisplayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> firstProbeObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> secondProbeObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> releaseTargets = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentBag<Task<RequestResult<string>>> probes = [];
        RequestDetailsFeature first = new(async (_, _) => await releaseTargets.Task);
        RequestDetailsFeature second = new(async (_, _) => await releaseTargets.Task);
        Mediator mediator = new();
        using IDisposable firstRegistration = mediator.Register(first.Load);
        using IDisposable secondRegistration = mediator.Register(second.Load);
        using RequestInlineProjection firstProjection = new(first);
        using RequestInlineProjection secondProjection = new(second);
        AttachProbe(firstProjection, second.Load, firstDisplayed, firstProbeObserved);
        AttachProbe(secondProjection, first.Load, secondDisplayed, secondProbeObserved);

        Task<RequestResult<string>> firstRequest = Task.Run(() => mediator.SendAsync(new LoadDetails(7), first.Load));
        Task<RequestResult<string>> secondRequest = Task.Run(() => mediator.SendAsync(new LoadDetails(7), second.Load));
        try
        {
            bool[] observedWithinCallbacks = await Task.WhenAll(firstProbeObserved.Task, secondProbeObserved.Task).WaitAsync(Watchdog);
            RequestResult<string>[] peerResponses = await Task.WhenAll(probes).WaitAsync(Watchdog);
            await Assert.That(observedWithinCallbacks.All(static observed => observed)).IsTrue();
            await Assert.That(peerResponses.Length).IsEqualTo(2);
            foreach (RequestResult<string> response in peerResponses)
            {
                await Assert.That(response.Kind).IsEqualTo(RequestResultKind.Responded);
                await Assert.That(response.OperationResult!.Kind).IsEqualTo(OperationResultKind.Rejected);
                await Assert.That(response.OperationResult.Reason).IsEqualTo("AlreadyRunning");
            }
        }
        finally
        {
            releaseTargets.TrySetResult("completed");
            await Task.WhenAll(firstRequest, secondRequest).WaitAsync(Watchdog);
            await Task.WhenAll(probes).WaitAsync(Watchdog);
        }

        void AttachProbe(RequestInlineProjection projection, RequestPort<LoadDetails, string> peer,
            TaskCompletionSource displayed, TaskCompletionSource<bool> probeObserved)
        {
            int handled = 0;
            projection.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(projection.Snapshot) || Interlocked.Exchange(ref handled, 1) != 0)
                {
                    return;
                }

                displayed.SetResult();
                Task.WhenAll(firstDisplayed.Task, secondDisplayed.Task).WaitAsync(Watchdog).GetAwaiter().GetResult();
                Task<RequestResult<string>> probe = Task.Run(() => mediator.SendAsync(new LoadDetails(7), peer));
                probes.Add(probe);
                try
                {
                    // 只为失败实现提供退出保护；先用两个显示信号确定双方回调已经进入。
                    probe.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                    probeObserved.SetResult(true);
                }
                catch (TimeoutException)
                {
                    probeObserved.SetResult(false);
                }
            };
        }
    }
}

internal sealed record LoadDetails(int ObjectId);

internal sealed record RequestDetailsState
{
    [Input]
    public bool Enabled { get; init; } = true;

    [Input]
    public string Draft { get; init; } = string.Empty;

    public int ObjectId { get; init; }

    public int TrackedUpdates { get; init; }

    public string Text { get; init; } = string.Empty;
}

internal sealed partial class RequestDetailsFeature : Feature<RequestDetailsState>
{
    private readonly Func<Operation<RequestDetailsState>, LoadDetails, ValueTask<string>> service;
    private readonly Func<RequestDetailsState, LoadDetails, bool> validate;

    public RequestDetailsFeature(Func<Operation<RequestDetailsState>, LoadDetails, ValueTask<string>>? service = null,
        Func<RequestDetailsState, LoadDetails, bool>? validate = null) : base(new())
    {
        this.service = service ?? (static (_, request) => ValueTask.FromResult($"details {request.ObjectId}"));
        this.validate = validate ?? (static (state, request) => state.Enabled && request.ObjectId > 0);
        Load = CreateRequestPort<LoadDetails, string>("Load", this.validate, HandleAsync);
    }

    public RequestPort<LoadDetails, string> Load { get; }

    public Task<OperationResult<string>> LoadAsync(LoadDetails request, CancellationToken cancellationToken = default)
        => DispatchOperation("Load", state => validate(state, request), operation => HandleAsync(operation, request), cancellationToken);

    private async ValueTask<string> HandleAsync(Operation<RequestDetailsState> operation, LoadDetails request)
    {
        string text = await service(operation, request);
        await operation.UpdateAsync(static (state, payload) => state with { ObjectId = payload.ObjectId, Text = payload.Text },
            (request.ObjectId, Text: text));
        return text;
    }
}

internal sealed record LookupReply(bool Found);

internal sealed partial class RequestBusinessFailureFeature : Feature<RequestDetailsState>
{
    public RequestBusinessFailureFeature() : base(new())
    {
        Lookup = CreateRequestPort<LoadDetails, LookupReply>("Lookup", null, static async (operation, _) =>
        {
            await operation.UpdateAsync(static (state, value) => state with { Text = value }, "not found");
            return new LookupReply(false);
        });
    }

    public RequestPort<LoadDetails, LookupReply> Lookup { get; }
}

internal sealed record RequestQueryState
{
    [Input]
    public int ObjectId { get; init; }

    public ImmutableArray<string> Responses { get; init; } = [];
}

internal sealed partial class RequestQueryFeature(Mediator mediator, params RequestPort<LoadDetails, string>[] targets)
    : Feature<RequestQueryState>(new())
{
    [Operation]
    private async ValueTask<ImmutableArray<RequestResult<string>>> QueryAsync(Operation<RequestQueryState> operation)
    {
        ImmutableArray<RequestResult<string>>.Builder responses = ImmutableArray.CreateBuilder<RequestResult<string>>();
        foreach (RequestPort<LoadDetails, string> target in targets)
        {
            RequestResult<string> response = await mediator.SendAsync(new LoadDetails(operation.Snapshot.ObjectId), target,
                operation.CancellationToken);
            responses.Add(response);
            await operation.UpdateAsync(static (state, value) => state with
            {
                Responses = state.Responses.Add(value.OperationResult?.Value ?? value.Kind.ToString()),
            }, response);
        }

        return responses.ToImmutable();
    }
}

internal sealed class RequestInlineProjection : FeatureProjection<RequestDetailsState>
{
    public RequestInlineProjection(RequestDetailsFeature feature)
        : base(feature, static callback => callback(), ProjectionMode.Coalesce)
        => InitializeProjection();

    protected override void OnSnapshotChanged(RequestDetailsState previous, RequestDetailsState current)
    {
    }
}
