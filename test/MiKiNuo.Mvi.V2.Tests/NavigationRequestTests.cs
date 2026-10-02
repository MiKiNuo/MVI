using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过真实请求链验证导航关闭发起者、执行有效性与释放等待边界。</summary>
public sealed class NavigationRequestTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>普通async展示事件的同步前半段返回后，不再保留已退出source的物理资源等待边。</summary>
    /// <returns>fresh边同步scope寿命验证任务。</returns>
    [Test]
    public async Task AsyncProjectionEventEndsPhysicalDependencyWhenItsSynchronousScopeReturns()
    {
        TaskCompletionSource sourceEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource sourceRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource targetEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource targetRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource handled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature source = new(async (_, _) => { sourceEntered.SetResult(); await sourceRelease.Task; return "source"; });
        RequestDetailsFeature target = new(async (_, _) =>
        {
            targetEntered.SetResult();
            await targetRelease.Task;
            await source.Close().Ticket.Released;
            return "source has exited";
        });
        Mediator mediator = new();
        using IDisposable route = mediator.Register(target.Load);
        using NavigationProjection projection = new(source, static callback => callback());
        int once = 0;
#pragma warning disable TUnit0031 // 原生async事件的同步前半段是本回归边界，异常通过handled明确观察。
        projection.PropertyChanged += async (_, _) =>
        {
            if (!source.Snapshot.OperationStates.TryGetValue("Load", out OperationState? state) || state.IsRunning
                || Interlocked.Exchange(ref once, 1) != 0) return;
            try
            {
                RequestResult<string> result = await mediator.SendAsync(new LoadDetails(1), target.Load);
                await Assert.That(result.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
                handled.SetResult();
            }
            catch (Exception failure) { handled.SetException(failure); }
        };
#pragma warning restore TUnit0031
        Task<OperationResult<string>> execution = source.LoadAsync(new(1));
        try
        {
            await sourceEntered.Task.WaitAsync(Watchdog);
            sourceRelease.SetResult();
            await execution.WaitAsync(Watchdog);
            await targetEntered.Task.WaitAsync(Watchdog);
            targetRelease.SetResult();
            await handled.Task.WaitAsync(Watchdog);
        }
        finally
        {
            sourceRelease.TrySetResult(); targetRelease.TrySetResult();
            await source.Close().Ticket.Released.WaitAsync(Watchdog);
            await target.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>同步结束展示回调仍在真实资源退出栈内，自释放读取应拒绝但fresh新消息可发送。</summary>
    /// <returns>同步展示资源与业务入口分离验证任务。</returns>
    [Test]
    public async Task SynchronousFreshProjectionPreservesResourceWaitGuard()
    {
        Mediator mediator = new();
        TaskCompletionSource serviceEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource serviceRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature source = new(async (_, _) => { serviceEntered.SetResult(); await serviceRelease.Task; return "source"; });
        RequestDetailsFeature target = new();
        using IDisposable registration = mediator.Register(target.Load);
        using NavigationProjection projection = new(source, static callback => callback());
        bool rejected = false;
        Task<RequestResult<string>>? sent = null;
        projection.PropertyChanged += (_, _) =>
        {
            if (!source.Snapshot.OperationStates.TryGetValue("Load", out OperationState? state) || state.IsRunning) return;
            try { _ = source.Close().Ticket.Released; } catch (InvalidOperationException) { rejected = true; }
            sent ??= mediator.SendAsync(new LoadDetails(2), target.Load);
        };
        Task<OperationResult<string>> execution = source.LoadAsync(new(1));
        await serviceEntered.Task.WaitAsync(Watchdog);
        serviceRelease.SetResult();
        await execution.WaitAsync(Watchdog);
        await Assert.That(rejected).IsTrue();
        await Assert.That((await sent!.WaitAsync(Watchdog)).OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        await source.Close().Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>同步fresh回调发起并同步等待Send时，目标仍识别实际调用栈的资源caller。</summary>
    /// <returns>同步新消息保留真实退出依赖验证任务。</returns>
    [Test]
    public async Task SynchronousFreshCallbackSendKeepsActualResourceCallerDependency()
    {
        Mediator mediator = new();
        TaskCompletionSource serviceEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource serviceRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature source = new(async (_, _) => { serviceEntered.SetResult(); await serviceRelease.Task; return "source"; });
        bool rejected = false;
        RequestDetailsFeature target = new((_, _) =>
        {
            try { _ = source.Close().Ticket.Released; } catch (InvalidOperationException) { rejected = true; }
            return ValueTask.FromResult("new message");
        });
        using IDisposable route = mediator.Register(target.Load);
        using NavigationProjection projection = new(source, static callback => callback());
        int once = 0;
        projection.PropertyChanged += (_, _) =>
        {
            if (source.Snapshot.OperationStates.TryGetValue("Load", out OperationState? state) && !state.IsRunning
                && Interlocked.Exchange(ref once, 1) == 0)
            {
                mediator.SendAsync(new LoadDetails(1), target.Load).WaitAsync(Watchdog).GetAwaiter().GetResult();
            }
        };
        Task<OperationResult<string>> execution = source.LoadAsync(new(1));
        await serviceEntered.Task.WaitAsync(Watchdog);
        serviceRelease.SetResult();
        await execution.WaitAsync(Watchdog);
        await Assert.That(rejected).IsTrue();
        await source.Close().Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>同步fresh回调排出的独立异步UI工作不继承线程限定的资源退出依赖。</summary>
    /// <returns>异步fresh入口不误拒验证任务。</returns>
    [Test]
    public async Task IndependentAsyncDisplayWorkDoesNotInheritSynchronousResourceStack()
    {
        RequestDetailsFeature source = new();
        using NavigationProjection projection = new(source, static callback => callback());
        TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int once = 0;
        projection.PropertyChanged += (_, _) =>
        {
            if (source.Snapshot.OperationStates.TryGetValue("Load", out OperationState? state) && !state.IsRunning
                && Interlocked.Exchange(ref once, 1) == 0)
            {
                _ = Task.Run(async () =>
                {
                    try { await source.Close().Ticket.Released; released.SetResult(); }
                    catch (Exception failure) { released.SetException(failure); }
                });
            }
        };
        await source.LoadAsync(new(1)).WaitAsync(Watchdog);
        await released.Task.WaitAsync(Watchdog);
    }

    /// <summary>静默确认活动可以经Send/Post/TryPost执行合法保存确认契约。</summary>
    /// <param name="mode">确认使用的消息入口。</param>
    /// <returns>确认帧真实归属消息有效性验证任务。</returns>
    [Test]
    [Arguments("send")]
    [Arguments("post")]
    [Arguments("try")]
    public async Task ConfirmationFramesCanSendBusinessMessages(string mode)
    {
        Mediator mediator = new();
        RequestDetailsFeature target = new();
        using IDisposable route = mediator.Register(target.Load);
        ConfirmationFeature feature = new(async context =>
        {
            if (mode == "send") return (await mediator.SendAsync(new LoadDetails(1), target.Load, CancellationToken.None)).OperationResult!.Kind == OperationResultKind.Completed;
            PostReceipt<string> receipt;
            if (mode == "post") receipt = mediator.Post(new LoadDetails(1), target.Load);
            else if (!mediator.TryPost(new LoadDetails(1), target.Load, out receipt)) return false;
            return (await receipt.Completion!).Kind == OperationResultKind.Completed;
        });
        CloseRequestResult result = await feature.RequestCloseAsync().WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
        await result.Close!.Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>中间调用者取消等待后，目标不再继承仍活动的更外层等待依赖。</summary>
    /// <returns>等待链中段截断验证任务。</returns>
    [Test]
    public async Task InactiveMiddleRequestCutsOffOuterDependency()
    {
        Mediator mediator = new();
        RequestDetailsFeature root = new();
        using CancellationTokenSource middleWait = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource middleStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseMiddle = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature inner = new(async (_, _) =>
        {
            entered.SetResult();
            await middleStopped.Task;
            Task<ReleaseResult> released;
            try { released = root.Close().Ticket.Released; allowed.SetResult(true); }
            catch (Exception failure) { allowed.SetException(failure); throw; }
            await released;
            return "inner continued";
        });
        RequestDetailsFeature middle = new(async (_, _) =>
        {
            await mediator.SendAsync(new LoadDetails(1), inner.Load, middleWait.Token);
            middleStopped.SetResult();
            await releaseMiddle.Task;
            return "middle done";
        });
        using IDisposable innerRoute = mediator.Register(inner.Load);
        using IDisposable middleRoute = mediator.Register(middle.Load);
        RequestDetailsFeature caller = new(async (_, _) => (await mediator.SendAsync(new LoadDetails(1), middle.Load)).OperationResult!.Value!);
        root.Children.Add(caller);
        Task<OperationResult<string>> call = caller.LoadAsync(new(1));
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            middleWait.Cancel();
            await middleStopped.Task.WaitAsync(Watchdog);
            await Assert.That(await allowed.Task.WaitAsync(Watchdog)).IsTrue();
        }
        finally
        {
            releaseMiddle.TrySetResult(); middleWait.Cancel();
            await call.WaitAsync(Watchdog);
            await root.Close().Ticket.Released.WaitAsync(Watchdog);
            await middle.Close().Ticket.Released.WaitAsync(Watchdog);
            await inner.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>导航确认拒绝或目标故障保持发起者可用，并通过原请求结果观察。</summary>
    /// <param name="fault">是否目标处理故障。</param>
    /// <returns>导航拒绝与故障关联验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task NavigationVetoAndTargetFailureRemainObservable(bool fault)
    {
        Mediator mediator = new();
        ConfirmationFeature caller = new(static _ => ValueTask.FromResult(false));
        InvalidOperationException failure = new("navigation target fault");
        RequestDetailsFeature target = new(async (_, _) =>
        {
            if (fault) throw failure;
            CloseRequestResult close = await caller.RequestCloseAsync();
            return close.Kind.ToString();
        });
        using IDisposable route = mediator.Register(target.Load);
        RequestResult<string> result = await mediator.SendAsync(new LoadDetails(1), target.Load).WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(RequestResultKind.Responded);
        await Assert.That(result.OperationResult!.Kind).IsEqualTo(fault ? OperationResultKind.Faulted : OperationResultKind.Completed);
        if (fault) await Assert.That(result.OperationResult.Exception).IsEqualTo(failure);
        else await Assert.That(result.OperationResult.Value).IsEqualTo("Rejected");
        await Assert.That(caller.IsClosed).IsFalse();
        caller.SetValue(4);
        await caller.Close().Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>Parallel 同名执行保持独立发送身份，一项完成不影响另一项合法消息。</summary>
    /// <returns>精确并行执行帧验证任务。</returns>
    [Test]
    public async Task ParallelFramesRemainIndependentWhenOneExecutionEnds()
    {
        Mediator mediator = new();
        RequestDetailsFeature target = new();
        using IDisposable route = mediator.Register(target.Load);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature source = new(async (value, token) =>
        {
            if (value == 1) { entered.SetResult(); await release.Task; }
            RequestResult<string> reply = await mediator.SendAsync(new LoadDetails(value), target.Load, CancellationToken.None);
            await Assert.That(reply.OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
            return value;
        });
        Task<OperationResult<int>> first = source.ParallelAsync();
        await entered.Task.WaitAsync(Watchdog);
        source.SetValue(2);
        await source.ParallelAsync().WaitAsync(Watchdog);
        release.SetResult();
        await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
        await source.Close().Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>导航逻辑关闭已接纳请求的发起者后先返回响应，其Scope等待业务及Track真实退出。</summary>
    /// <returns>导航响应与释放先后验证任务。</returns>
    [Test]
    public async Task NavigationResponsePrecedesRequesterAndTrackedResourceRelease()
    {
        Mediator mediator = new();
        TaskCompletionSource responseObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LifecycleFeature? requester = null;
        RequestDetailsFeature target = new(async (_, _) =>
        {
            CloseRequestResult closed = await requester!.RequestCloseAsync();
            await Assert.That(closed.Kind).IsEqualTo(CloseRequestKind.Closed);
            await Assert.That(() => { _ = closed.Close!.Ticket.Released; }).Throws<InvalidOperationException>();
            return "navigated";
        });
        using IDisposable route = mediator.Register(target.Load);
        await using ServiceProvider provider = LifetimeServices.Create(new(async (operation, value, resource) =>
        {
            operation.Track(ChildAsync(resource));
            RequestResult<string> result = await mediator.SendAsync(new LoadDetails(1), target.Load);
            await Assert.That(result.OperationResult!.Value).IsEqualTo("navigated");
            resource.Use();
            responseObserved.SetResult();
            await release.Task;
            resource.Use();
            return value;
        }));
        requester = await FeatureFactory.CreateAsync<LifecycleFeature>(provider);
        Task<OperationResult<int>> running = requester.RejectAsync();
        try
        {
            await responseObserved.Task.WaitAsync(Watchdog);
            CloseResult close = requester.Close();
            await Assert.That(requester.IsClosed).IsTrue();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(requester.Resource.DisposeCount).IsEqualTo(0);
            release.SetResult();
            await Assert.That(running.IsCompleted).IsFalse();
            releaseChild.SetResult();
            await running.WaitAsync(Watchdog);
            await close.Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(requester.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult(); releaseChild.TrySetResult();
            await running.WaitAsync(Watchdog);
            await requester.Close().Ticket.Released.WaitAsync(Watchdog);
            await target.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task ChildAsync(LifetimeResource resource) { await releaseChild.Task; resource.Use(); }
    }

    /// <summary>多层目标不能等待最初调用者祖先释放，明确取消等待后边失效且目标继续。</summary>
    /// <returns>活动请求链与结束边验证任务。</returns>
    [Test]
    public async Task MultiLevelRequestDependenciesEndWhenCallerStopsWaiting()
    {
        Mediator mediator = new();
        using CancellationTokenSource wait = new();
        TaskCompletionSource innerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource afterCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature parent = new();
        RequestDetailsFeature? caller = null;
        RequestDetailsFeature inner = new(async (_, _) =>
        {
            CloseResult close = parent.Close();
            bool rejected = false;
            try { _ = close.Ticket.Released; } catch (InvalidOperationException) { rejected = true; }
            innerEntered.SetResult();
            await afterCanceled.Task;
            await close.Ticket.Released;
            observed.SetResult(rejected);
            return "after caller release";
        });
        RequestDetailsFeature middle = new(async (_, _) =>
            (await mediator.SendAsync(new LoadDetails(1), inner.Load)).OperationResult!.Value!);
        using IDisposable innerRoute = mediator.Register(inner.Load);
        using IDisposable middleRoute = mediator.Register(middle.Load);
        caller = new(async (_, _) =>
        {
            RequestResult<string> result = await mediator.SendAsync(new LoadDetails(1), middle.Load, wait.Token);
            await Assert.That(result.Kind).IsEqualTo(RequestResultKind.WaitCanceled);
            return "wait ended";
        });
        parent.Children.Add(caller);
        Task<OperationResult<string>> execution = caller.LoadAsync(new(1));
        try
        {
            await innerEntered.Task.WaitAsync(Watchdog);
            wait.Cancel();
            await execution.WaitAsync(Watchdog);
            afterCanceled.SetResult();
            await Assert.That(await observed.Task.WaitAsync(Watchdog)).IsTrue();
        }
        finally
        {
            wait.Cancel(); afterCanceled.TrySetResult();
            await parent.Close().Ticket.Released.WaitAsync(Watchdog);
            await middle.Close().Ticket.Released.WaitAsync(Watchdog);
            await inner.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>Latest身份失效即阻止后续Send/Post/TryPost，不依赖取消回调是否已送达。</summary>
    /// <param name="message">旧执行尝试的消息入口。</param>
    /// <returns>失效操作消息接纳保护验证任务。</returns>
    [Test]
    [Arguments("send")]
    [Arguments("post")]
    [Arguments("try")]
    public async Task SupersededExecutionCannotStartAnyNewMessage(string message)
    {
        Mediator mediator = new();
        int calls = 0;
        RequestDetailsFeature target = new((_, _) => { Interlocked.Increment(ref calls); return ValueTask.FromResult("target"); });
        using IDisposable route = mediator.Register(target.Load);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PolicyPortFeature source = new(async (value, token) =>
        {
            if (value == 1)
            {
                entered.SetResult(); await release.Task;
                if (message == "send") await mediator.SendAsync(new LoadDetails(1), target.Load, CancellationToken.None);
                else if (message == "post") mediator.Post(new LoadDetails(1), target.Load);
                else mediator.TryPost(new LoadDetails(1), target.Load, out _);
            }
            return value;
        });
        Task<OperationResult<int>> first = source.LatestAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            source.SetValue(2);
            await source.LatestAsync().WaitAsync(Watchdog);
            release.SetResult();
            await Assert.That((await first.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Superseded);
            await Assert.That(calls).IsEqualTo(0);
        }
        finally { release.TrySetResult(); await first.WaitAsync(Watchdog); await source.Close().Ticket.Released.WaitAsync(Watchdog); }
        await target.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>已结束操作同步及排队展示回调是新入口，可继续输入与发起新的业务请求。</summary>
    /// <param name="queued">是否排队展示回调。</param>
    /// <returns>展示入口不继承旧操作身份验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ProjectionCallbacksUseFreshMessageContext(bool queued)
    {
        Mediator mediator = new();
        RequestDetailsFeature target = new();
        using IDisposable registration = mediator.Register(target.Load);
        RequestDetailsFeature source = new();
        System.Collections.Concurrent.ConcurrentQueue<Action> callbacks = new();
        using NavigationProjection projection = new(source, queued ? callbacks.Enqueue : static callback => callback());
        TaskCompletionSource<RequestResult<string>> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int once = 0;
        projection.PropertyChanged += (_, _) =>
        {
            if (source.Snapshot.OperationStates.TryGetValue("Load", out OperationState? state) && !state.IsRunning
                && Interlocked.Exchange(ref once, 1) == 0)
            {
                source.SetDraft("fresh UI input");
                _ = SendAsync();
            }
        };
        await source.LoadAsync(new(1)).WaitAsync(Watchdog);
        while (callbacks.TryDequeue(out Action? callback)) callback();
        await Assert.That((await response.Task.WaitAsync(Watchdog)).OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(source.Snapshot.State.Draft).IsEqualTo("fresh UI input");
        await source.Close().Ticket.Released.WaitAsync(Watchdog);
        await target.Close().Ticket.Released.WaitAsync(Watchdog);

        async Task SendAsync() { try { response.SetResult(await mediator.SendAsync(new LoadDetails(2), target.Load)); } catch (Exception e) { response.SetException(e); } }
    }
}

internal sealed class NavigationProjection : FeatureProjection<RequestDetailsState>
{
    internal NavigationProjection(RequestDetailsFeature feature, Action<Action> schedule) : base(feature, schedule, ProjectionMode.Coalesce)
        => InitializeProjection();
    protected override void OnSnapshotChanged(RequestDetailsState previous, RequestDetailsState current) { }
}
