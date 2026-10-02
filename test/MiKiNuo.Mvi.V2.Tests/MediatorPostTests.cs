using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开端口验证实例共享收件箱的接纳、处理和关闭归属。</summary>
public sealed class MediatorPostTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>端口停用不能越过正在等待实例纯入箱提交的 Post，已入箱消息继续完成。</summary>
    /// <returns>端口停用与实例接纳排序验证任务。</returns>
    [Test]
    public async Task DeactivateCannotFinishAheadOfThePostAdmissionDecision()
    {
        TaskCompletionSource reducing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource postEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseReduction = new();
        PostFeature feature = new(1);
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.First);
        Task edit = Task.Run(() => feature.HoldInput(() =>
        {
            reducing.SetResult();
            if (!releaseReduction.Wait(Watchdog)) throw new TimeoutException("纯输入屏障未释放。");
        }));
        Task<PostReceipt<int>>? posting = null;
        Task? stopping = null;
        try
        {
            await reducing.Task.WaitAsync(Watchdog);
            posting = Task.Run(() =>
            {
                postEntered.SetResult();
                return mediator.Post(new FirstPost(1), feature.First);
            });
            await postEntered.Task.WaitAsync(Watchdog);
            stopping = Task.Run(() => { stopEntered.SetResult(); feature.First.Deactivate(); });
            await stopEntered.Task.WaitAsync(Watchdog);
            // 门闩持有时 Post 尚无入箱回执；停用若先完成，则后续入箱必须拒绝。
            bool stoppedBeforeAdmission = stopping.IsCompleted;
            releaseReduction.Set();
            await edit.WaitAsync(Watchdog);
            await stopping.WaitAsync(Watchdog);
            PostReceipt<int> receipt = await posting.WaitAsync(Watchdog);
            if (stoppedBeforeAdmission)
            {
                await Assert.That(receipt.Kind).IsEqualTo(PostResultKind.TargetUnavailable);
            }
            if (receipt.Kind == PostResultKind.Accepted)
            {
                await Assert.That((await receipt.Completion!.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Completed);
            }

            await Assert.That(mediator.Post(new FirstPost(2), feature.First).Kind).IsEqualTo(PostResultKind.TargetUnavailable);
        }
        finally
        {
            releaseReduction.Set();
            await edit.WaitAsync(Watchdog);
            if (posting is not null) await posting.WaitAsync(Watchdog);
            if (stopping is not null) await stopping.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>停用已经提交时，即使实例门仍被纯转换占用，Post 也直接拒绝且不等待实例门。</summary>
    /// <returns>停用先完成的明确拒绝验证任务。</returns>
    [Test]
    public async Task DeactivatedPortRejectsWithoutWaitingForTheStoreGate()
    {
        TaskCompletionSource reducing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseReduction = new();
        PostFeature feature = new(1);
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.First);
        feature.First.Deactivate();
        Task edit = Task.Run(() => feature.HoldInput(() =>
        {
            reducing.SetResult();
            if (!releaseReduction.Wait(Watchdog)) throw new TimeoutException("纯输入屏障未释放。");
        }));
        try
        {
            await reducing.Task.WaitAsync(Watchdog);
            PostReceipt<int> rejected = await Task.Run(() => mediator.Post(new FirstPost(1), feature.First)).WaitAsync(Watchdog);
            await Assert.That(rejected.Kind).IsEqualTo(PostResultKind.TargetUnavailable);
            await Assert.That(rejected.Completion is null).IsTrue();
        }
        finally
        {
            releaseReduction.Set();
            await edit.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>接纳回执不等待业务，两个契约共享含处理项的容量并按 FIFO 处理。</summary>
    /// <returns>共享收件箱验证任务。</returns>
    [Test]
    public async Task AdmissionIsSeparateFromProcessingAndCapacityIsSharedAcrossPorts()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PostFeature feature = new(2, async (_, value) =>
        {
            if (value == 1) { entered.SetResult(); await release.Task; }
            return value;
        });
        Mediator mediator = new();
        using IDisposable firstRoute = mediator.Register(feature.First);
        using IDisposable secondRoute = mediator.Register(feature.Second);
        PostReceipt<int> first = mediator.Post(new FirstPost(1), feature.First);
        try
        {
            await Assert.That(first.Kind).IsEqualTo(PostResultKind.Accepted);
            await entered.Task.WaitAsync(Watchdog);
            await Assert.That(first.Completion!.IsCompleted).IsFalse();
            bool accepted = mediator.TryPost(new SecondPost(2), feature.Second, out PostReceipt<int> second);
            bool full = mediator.TryPost(new FirstPost(3), feature.First, out PostReceipt<int> rejected);
            await Assert.That(accepted).IsTrue();
            await Assert.That(full).IsFalse();
            await Assert.That(rejected.Kind).IsEqualTo(PostResultKind.InboxFull);
            await Assert.That(rejected.Completion is null).IsTrue();
            await Assert.That(new[] { first.Id, second.Id, rejected.Id }.Distinct().Count()).IsEqualTo(3);
            release.SetResult();
            OperationResult<int> firstResult = await first.Completion.WaitAsync(Watchdog);
            OperationResult<int> secondResult = await second.Completion!.WaitAsync(Watchdog);
            await Assert.That(firstResult.OperationId).IsEqualTo(first.Id);
            await Assert.That(secondResult.OperationId).IsEqualTo(second.Id);
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([1, 2])).IsTrue();
            PostReceipt<int> restored = mediator.Post(new FirstPost(4), feature.First);
            await Assert.That(restored.Kind).IsEqualTo(PostResultKind.Accepted);
            await restored.Completion!.WaitAsync(Watchdog);
        }
        finally
        {
            release.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>处理时验证当前状态，验证拒绝、验证故障及服务故障都关联原回执且继续推进。</summary>
    /// <param name="outcome">等待投递的失败类型。</param>
    /// <returns>关联处理结果验证任务。</returns>
    [Test]
    [Arguments("validation")]
    [Arguments("validationFault")]
    [Arguments("serviceFault")]
    public async Task ProcessingUsesCurrentStateAndPreservesAssociatedFailures(string outcome)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("post service fault");
        ConcurrentQueue<int> calls = new();
        PostFeature feature = new(3, async (_, value) =>
        {
            calls.Enqueue(value);
            if (value == 1) { entered.SetResult(); await release.Task; }
            if (value == 2 && outcome == "serviceFault") throw failure;
            return value;
        });
        Mediator mediator = new();
        using IDisposable route = mediator.Register(feature.First);
        PostReceipt<int> first = mediator.Post(new FirstPost(1), feature.First);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            PostReceipt<int> queued = mediator.Post(new FirstPost(2), feature.First);
            PostReceipt<int> after = mediator.Post(new FirstPost(3), feature.First);
            await Assert.That(queued.Kind).IsEqualTo(PostResultKind.Accepted);
            feature.SetEnabled(outcome != "validation");
            feature.SetThrowValidation(outcome == "validationFault");
            release.SetResult();
            await first.Completion!.WaitAsync(Watchdog);
            OperationResult<int> rejected = await queued.Completion!.WaitAsync(Watchdog);
            OperationResult<int> next = await after.Completion!.WaitAsync(Watchdog);
            await Assert.That(rejected.OperationId).IsEqualTo(queued.Id);
            await Assert.That(rejected.Kind).IsEqualTo(outcome == "validation" ? OperationResultKind.Rejected : OperationResultKind.Faulted);
            await Assert.That(rejected.Reason).IsEqualTo(outcome == "serviceFault" ? null
                : outcome == "validation" ? "ValidationFailed" : "ValidationFault");
            if (outcome == "serviceFault") await Assert.That(rejected.Exception).IsEqualTo(failure);
            await Assert.That(next.Kind).IsEqualTo(OperationResultKind.Completed);
            await Assert.That(calls.SequenceEqual(outcome == "serviceFault" ? [1, 2, 3] : [1, 3])).IsTrue();
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([1, 3])).IsTrue();
        }
        finally
        {
            release.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>同一实例的并发投递竞争唯一有限容量，不丢拒绝原因。</summary>
    /// <returns>并发容量接纳验证任务。</returns>
    [Test]
    public async Task ConcurrentPostsAtomicallyCompeteForSharedCapacity()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource race = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PostFeature feature = new(3, async (_, value) => { await release.Task; return value; });
        Mediator mediator = new();
        using IDisposable route = mediator.Register(feature.First);
        Task<PostReceipt<int>>[] attempts = Enumerable.Range(1, 16).Select(value => Task.Run(async () =>
        {
            await race.Task;
            return mediator.Post(new FirstPost(value), feature.First);
        })).ToArray();
        try
        {
            race.SetResult();
            PostReceipt<int>[] receipts = await Task.WhenAll(attempts).WaitAsync(Watchdog);
            await Assert.That(receipts.Count(receipt => receipt.Kind == PostResultKind.Accepted)).IsEqualTo(3);
            await Assert.That(receipts.Count(receipt => receipt.Kind == PostResultKind.InboxFull)).IsEqualTo(13);
            release.SetResult();
            await Task.WhenAll(receipts.Where(receipt => receipt.Completion is not null).Select(receipt => receipt.Completion!)).WaitAsync(Watchdog);
            await Assert.That(feature.Snapshot.State.Values.Count).IsEqualTo(3);
        }
        finally
        {
            race.TrySetResult();
            release.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>两个目标明确分派且容量及状态独立，未接线、解绑与停用端口明确拒绝。</summary>
    /// <returns>目标选择和路由关闭验证任务。</returns>
    [Test]
    public async Task ExplicitTargetsRemainIsolatedAndUnavailableRoutesAreRejected()
    {
        PostFeature first = new(1);
        PostFeature second = new(1);
        Mediator mediator = new();
        PostReceipt<int> missing = mediator.Post(new FirstPost(1), first.First);
        await Assert.That(missing.Kind).IsEqualTo(PostResultKind.TargetUnavailable);
        using IDisposable firstRoute = mediator.Register(first.First);
        using IDisposable secondRoute = mediator.Register(second.First);
        PostReceipt<int> left = mediator.Post(new FirstPost(7), first.First);
        PostReceipt<int> right = mediator.Post(new FirstPost(8), second.First);
        await Task.WhenAll(left.Completion!, right.Completion!).WaitAsync(Watchdog);
        await Assert.That(first.Snapshot.State.Values.SequenceEqual([7])).IsTrue();
        await Assert.That(second.Snapshot.State.Values.SequenceEqual([8])).IsTrue();
        firstRoute.Dispose();
        await Assert.That(mediator.Post(new FirstPost(9), first.First).Kind).IsEqualTo(PostResultKind.TargetUnavailable);
        second.First.Deactivate();
        await Assert.That(mediator.Post(new FirstPost(9), second.First).Kind).IsEqualTo(PostResultKind.TargetUnavailable);
        await first.Close().Ticket.Released.WaitAsync(Watchdog);
        await second.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>Post 与 Send、程序调用共用同名并发入口，不能绕过重复拒绝。</summary>
    /// <returns>同名统一操作入口验证任务。</returns>
    [Test]
    public async Task PostedProcessingSharesOperationAdmissionWithSendAndDirectCalls()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PostFeature feature = new(2, async (_, value) => { entered.SetResult(); await release.Task; return value; });
        Mediator mediator = new();
        using IDisposable route = mediator.Register(feature.First);
        Task<OperationResult<int>> direct = feature.DirectAsync(1);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            PostReceipt<int> posted = mediator.Post(new FirstPost(2), feature.First);
            await Assert.That(posted.Kind).IsEqualTo(PostResultKind.Accepted);
            OperationResult<int> processed = await posted.Completion!.WaitAsync(Watchdog);
            await Assert.That(processed.Kind).IsEqualTo(OperationResultKind.Rejected);
            await Assert.That(processed.Reason).IsEqualTo("AlreadyRunning");
            RequestResult<int> sent = await mediator.SendAsync(new FirstPost(3), feature.First);
            await Assert.That(sent.OperationResult!.Reason).IsEqualTo("AlreadyRunning");
        }
        finally
        {
            release.TrySetResult();
            await direct.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>Post 的处理项可以进入既有操作 Queue，后续投递仍等待其真实结果后 FIFO 推进。</summary>
    /// <returns>两层接纳边界共用操作 Queue 验证任务。</returns>
    [Test]
    public async Task InboxProcessingReusesTheExistingOperationQueue()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<int> starts = new();
        PolicyPortFeature feature = new(async (value, _) =>
        {
            starts.Enqueue(value);
            if (value == 1) { entered.SetResult(); await release.Task; }
            return value;
        });
        RequestPort<PolicyPortRequest, int> port = feature.CreatePort("QueueAsync", OperationConcurrency.Queue, capacity: 2);
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(port);
        Task<OperationResult<int>> direct = feature.QueueAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            PostReceipt<int> first = mediator.Post(new PolicyPortRequest(2), port);
            PostReceipt<int> second = mediator.Post(new PolicyPortRequest(3), port);
            await Assert.That(first.Kind).IsEqualTo(PostResultKind.Accepted);
            await Assert.That(second.Kind).IsEqualTo(PostResultKind.Accepted);
            release.SetResult();
            await direct.WaitAsync(Watchdog);
            await first.Completion!.WaitAsync(Watchdog);
            await second.Completion!.WaitAsync(Watchdog);
            await Assert.That(starts.SequenceEqual([1, 2, 3])).IsTrue();
            await Assert.That(feature.Snapshot.State.Values.SequenceEqual([1, 2, 3])).IsTrue();
        }
        finally
        {
            release.TrySetResult();
            await direct.WaitAsync(Watchdog);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>服务构造调用关联投影触发真实关闭时，收件箱尚未消费的消息绝不能再启动。</summary>
    /// <returns>出箱与下一项启动之间确定关闭交错验证任务。</returns>
    [Test]
    public async Task CloseAtCurrentOperationCompletionPreventsTheNextPostFromStarting()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<int> starts = new();
        PostFeature feature = new(2, async (_, value) =>
        {
            starts.Enqueue(value);
            entered.TrySetResult();
            await release.Task;
            return value;
        });
        using PostCompletionProjection view = new(feature);
        CloseResult? closed = null;
        view.PropertyChanged += (_, _) =>
        {
            if (feature.Snapshot.OperationStates.TryGetValue("Process", out OperationState? operation)
                && !operation.IsRunning && operation.LastResult == OperationResultKind.Completed)
            {
                closed = feature.Close();
            }
        };
        Mediator mediator = new();
        using IDisposable registration = mediator.Register(feature.First);
        PostReceipt<int> first = mediator.Post(new FirstPost(1), feature.First);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            PostReceipt<int> second = mediator.Post(new FirstPost(2), feature.First);
            release.SetResult();
            await first.Completion!.WaitAsync(Watchdog);
            OperationResult<int> result = await second.Completion!.WaitAsync(Watchdog);
            await Assert.That(closed).IsNotNull();
            await Assert.That(result.Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(result.OperationId).IsEqualTo(second.Id);
            await closed!.Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(starts.SequenceEqual([1])).IsTrue();
        }
        finally
        {
            release.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>真实关闭拒绝后续投递，排队消息终结且不启动，不合作工作及嵌套 Track 保留 Scope。</summary>
    /// <returns>投递关闭与资源退出验证任务。</returns>
    [Test]
    public async Task CloseCancelsWaitingPostsAndRetainsRunningAndTrackedResources()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource nestedEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<int> calls = new();
        ServiceCollection services = new();
        services.AddScoped<LifetimeResource>();
        services.AddSingleton(new PostWork(async (operation, value, resource) =>
        {
            calls.Enqueue(value);
            entered.TrySetResult();
            await release.Task;
            resource.Use();
            operation.Track(NestedAsync(resource));
            return value;
        }));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        ScopedPostFeature feature = await FeatureFactory.CreateAsync<ScopedPostFeature>(provider);
        Mediator mediator = new();
        using IDisposable route = mediator.Register(feature.Port);
        PostReceipt<int> running = mediator.Post(new FirstPost(1), feature.Port);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            PostReceipt<int> waiting = mediator.Post(new FirstPost(2), feature.Port);
            CloseResult close = feature.Close();
            await Assert.That(mediator.Post(new FirstPost(3), feature.Port).Kind).IsEqualTo(PostResultKind.TargetUnavailable);
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            OperationResult<int> canceled = await waiting.Completion!.WaitAsync(Watchdog);
            await Assert.That(canceled.Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That(canceled.OperationId).IsEqualTo(waiting.Id);
            release.SetResult();
            await nestedEntered.Task.WaitAsync(Watchdog);
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(running.Completion!.IsCompleted).IsFalse();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            releaseNested.SetResult();
            await Assert.That((await running.Completion.WaitAsync(Watchdog)).Kind).IsEqualTo(OperationResultKind.Canceled);
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(feature.Resource.DisposeCount).IsEqualTo(1);
            await Assert.That(calls.SequenceEqual([1])).IsTrue();
        }
        finally
        {
            release.TrySetResult();
            releaseNested.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task NestedAsync(LifetimeResource resource)
        {
            nestedEntered.SetResult();
            await releaseNested.Task;
            resource.Use();
        }
    }

    /// <summary>关闭与入箱竞争时，所有已接纳消息都有结果，释放完成后不存在仍访问范围的执行。</summary>
    /// <returns>接纳调度关闭空隙验证任务。</returns>
    [Test]
    public async Task AdmissionSchedulingAndCloseRaceAlwaysTerminatesAcceptedReceipts()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            TaskCompletionSource race = new(TaskCreationOptions.RunContinuationsAsynchronously);
            PostFeature feature = new(2);
            Mediator mediator = new();
            using IDisposable route = mediator.Register(feature.First);
            Task<PostReceipt<int>> post = Task.Run(async () => { await race.Task; return mediator.Post(new FirstPost(1), feature.First); });
            Task<CloseResult> close = Task.Run(async () => { await race.Task; return feature.Close(); });
            race.SetResult();
            PostReceipt<int> receipt = await post.WaitAsync(Watchdog);
            CloseResult closed = await close.WaitAsync(Watchdog);
            await closed.Ticket.Released.WaitAsync(Watchdog);
            if (receipt.Kind == PostResultKind.Accepted)
            {
                await Assert.That(receipt.Completion!.IsCompleted).IsTrue();
                OperationResult<int> result = await receipt.Completion;
                await Assert.That(result.OperationId).IsEqualTo(receipt.Id);
                await Assert.That(result.Kind is OperationResultKind.Completed or OperationResultKind.Canceled or OperationResultKind.Rejected).IsTrue();
            }
            else
            {
                await Assert.That(receipt.Kind).IsEqualTo(PostResultKind.TargetUnavailable);
            }
        }
    }

    /// <summary>实例收件箱拒绝非正数容量。</summary>
    /// <returns>容量声明验证任务。</returns>
    [Test]
    public async Task InboxCapacityMustBePositive()
        => await Assert.That(() => new PostFeature(0)).Throws<ArgumentOutOfRangeException>();
}

internal sealed record FirstPost(int Value);
internal sealed record SecondPost(int Value);

internal sealed record PostState
{
    [Input]
    public bool Enabled { get; init; } = true;
    [Input]
    public bool ThrowValidation { get; init; }
    public ImmutableList<int> Values { get; init; } = [];
}

internal sealed partial class PostFeature : Feature<PostState>
{
    private readonly Func<Operation<PostState>, int, ValueTask<int>> service;

    internal PostFeature(int capacity, Func<Operation<PostState>, int, ValueTask<int>>? service = null) : base(new(), capacity)
    {
        this.service = service ?? (static (_, value) => ValueTask.FromResult(value));
        First = CreateRequestPort<FirstPost, int>("Process", static (state, message) => Validate(state, message.Value),
            (operation, message) => RunAsync(operation, message.Value));
        Second = CreateRequestPort<SecondPost, int>("Process", null, (operation, message) => RunAsync(operation, message.Value));
    }

    internal RequestPort<FirstPost, int> First { get; }
    internal RequestPort<SecondPost, int> Second { get; }
    internal Task<OperationResult<int>> DirectAsync(int value)
        => DispatchOperation("Process", null, operation => RunAsync(operation, value), CancellationToken.None);

    internal void HoldInput(Action barrier)
        => DispatchInput(barrier, static (state, hold) => { hold(); return state; });

    private static bool Validate(PostState state, int value)
        => value != 2 || (state.ThrowValidation ? throw new InvalidOperationException("post validation fault") : state.Enabled);

    private async ValueTask<int> RunAsync(Operation<PostState> operation, int value)
    {
        int result = await service(operation, value);
        await operation.UpdateAsync(static (state, item) => state with { Values = state.Values.Add(item) }, result);
        return result;
    }
}

internal sealed class PostWork(Func<Operation<PostState>, int, LifetimeResource, ValueTask<int>> run)
{
    internal Func<Operation<PostState>, int, LifetimeResource, ValueTask<int>> Run { get; } = run;
}

internal sealed partial class ScopedPostFeature : Feature<PostState>
{
    public ScopedPostFeature(LifetimeResource resource, PostWork work) : base(new(), 2)
    {
        Resource = resource;
        Port = CreateRequestPort<FirstPost, int>("Process", null,
            (operation, message) => work.Run(operation, message.Value, resource));
    }

    internal LifetimeResource Resource { get; }
    internal RequestPort<FirstPost, int> Port { get; }
}

internal sealed class PostCompletionProjection : FeatureProjection<PostState>
{
    internal PostCompletionProjection(PostFeature feature) : base(feature, static callback => callback(), ProjectionMode.Coalesce)
        => InitializeProjection();

    protected override void OnSnapshotChanged(PostState previous, PostState current)
    {
    }
}
