using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开关闭请求验证整树准备、条件重验及确认执行的资源归属。</summary>
public sealed class CloseConfirmationTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>正在确认的子实例退出时重验剩余树，不伪报调用方等待取消。</summary>
    /// <returns>实际确认成员退出结果验证任务。</returns>
    [Test]
    public async Task RemovingTheCurrentlyConfirmingChildRepreparesRemainingTree()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfirmationFeature root = new(static _ => ValueTask.FromResult(true));
        ConfirmationFeature child = new(async _ => { entered.SetResult(); await release.Task; return true; });
        root.Children.Add(child);
        Task<CloseRequestResult> request = root.RequestCloseAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            root.Children.Remove(child);
            release.SetResult();
            CloseRequestResult result = await request.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
            await result.Close!.Ticket.Released.WaitAsync(Watchdog);
        }
        finally { release.TrySetResult(); await root.Close().Ticket.Released.WaitAsync(Watchdog); }
    }

    /// <summary>另一个请求先提交后取消未合作确认等待，应返回原关闭票据并仍等待确认真实退出。</summary>
    /// <returns>并发提交胜出验证任务。</returns>
    [Test]
    public async Task ConcurrentCommittedCloseWinsOverLaterWaitCancellation()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        ConfirmationFeature root = new(async _ =>
        {
            if (Interlocked.Increment(ref calls) == 1) { entered.SetResult(); return await release.Task; }
            return true;
        });
        using CancellationTokenSource waiting = new();
        Task<CloseRequestResult> first = root.RequestCloseAsync(waiting.Token);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseRequestResult winner = await root.RequestCloseAsync().WaitAsync(Watchdog);
            await Assert.That(winner.Kind).IsEqualTo(CloseRequestKind.Closed);
            waiting.Cancel();
            CloseRequestResult after = await first.WaitAsync(Watchdog);
            await Assert.That(after.Kind).IsEqualTo(CloseRequestKind.Closed);
            await Assert.That(ReferenceEquals(after.Close!.Ticket, winner.Close!.Ticket)).IsTrue();
            await Assert.That(winner.Close.Ticket.Released.IsCompleted).IsFalse();
            release.SetResult(true);
            await winner.Close.Ticket.Released.WaitAsync(Watchdog);
        }
        finally { release.TrySetResult(true); await root.Close().Ticket.Released.WaitAsync(Watchdog); }
    }

    /// <summary>任一拒绝或故障保留所有实例、输入及路由，全部同意才整体关闭。</summary>
    /// <param name="fault">是否确认故障。</param>
    /// <returns>整树确认结果验证任务。</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RejectionOrFaultKeepsTreeAndRoutesUsable(bool fault)
    {
        ConfirmationFeature root = new(static _ => ValueTask.FromResult(true));
        ConfirmationFeature first = new(static _ => ValueTask.FromResult(true));
        ConfirmationFeature second = new(_ => fault ? ValueTask.FromException<bool>(new InvalidOperationException("confirm fault")) : ValueTask.FromResult(false));
        root.Children.Add(first);
        FeatureMember member = root.Children.Add(second);
        Mediator mediator = new();
        member.Wire(mediator, second.Port);
        CloseRequestResult result = await root.RequestCloseAsync().WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(fault ? CloseRequestKind.Faulted : CloseRequestKind.Rejected);
        await Assert.That(root.IsClosed || first.IsClosed || second.IsClosed).IsFalse();
        second.SetValue(4);
        await Assert.That((await mediator.SendAsync(new ConfirmationMessage(5), second.Port)).OperationResult!.Kind).IsEqualTo(OperationResultKind.Completed);
        PostReceipt<int> post = mediator.Post(new ConfirmationMessage(6), second.Port);
        await Assert.That(post.Kind).IsEqualTo(PostResultKind.Accepted);
        await post.Completion!.WaitAsync(Watchdog);
        await root.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>确认期间业务编辑及嵌套成员变化使旧同意失效并重新准备完整树。</summary>
    /// <param name="change">本轮确认期间的变化类型。</param>
    /// <returns>条件版本重验任务。</returns>
    [Test]
    [Arguments("edit")]
    [Arguments("add")]
    [Arguments("remove")]
    public async Task StateAndMembershipChangesRequireACompleteNewPreparation(string change)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        ConfirmationFeature root = new(async context =>
        {
            if (Interlocked.Increment(ref calls) == 1) { entered.SetResult(); return await response.Task; }
            return true;
        });
        ConfirmationFeature child = new(static _ => ValueTask.FromResult(true));
        root.Children.Add(child);
        Task<CloseRequestResult> request = root.RequestCloseAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            if (change == "edit") root.SetValue(9);
            else if (change == "add") child.Children.Add(new ConfirmationFeature(static _ => ValueTask.FromResult(true)));
            else root.Children.Remove(child);
            response.SetResult(true);
            CloseRequestResult result = await request.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
            await Assert.That(calls).IsEqualTo(2);
            await result.Close!.Ticket.Released.WaitAsync(Watchdog);
        }
        finally
        {
            response.TrySetResult(true);
            await root.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>仅操作Busy及完成变化不使业务条件失效，确认回调仍能合法编辑而不死锁。</summary>
    /// <returns>业务条件版本独立验证任务。</returns>
    [Test]
    public async Task RuntimeOperationChangesDoNotRepeatBusinessConfirmation()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        ConfirmationFeature feature = new(async _ => { calls++; entered.SetResult(); return await response.Task; });
        Task<CloseRequestResult> request = feature.RequestCloseAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            await feature.NoChangeAsync().WaitAsync(Watchdog);
            feature.SetValue(0); // 生成新的相等record快照，不改变业务条件。
            response.SetResult(true);
            CloseRequestResult result = await request.WaitAsync(Watchdog);
            await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
            await Assert.That(calls).IsEqualTo(1);
            await result.Close!.Ticket.Released.WaitAsync(Watchdog);
        }
        finally
        {
            response.TrySetResult(true);
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>取消等待不释放仍运行的确认IO和子工作，另一个关闭也必须等真实退出。</summary>
    /// <returns>确认工作范围资源验证任务。</returns>
    [Test]
    public async Task CanceledWaitAndAnotherCloseKeepUncooperativeConfirmationAndTrackedResourcesAlive()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource childEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseChild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LifetimeResource? resource = null;
        ServiceCollection services = new();
        services.AddScoped(_ => resource = new LifetimeResource());
        services.AddSingleton(new ConfirmationWork(async (context, dependency) =>
        {
            entered.SetResult();
            await release.Task;
            dependency.Use();
            context.Track(ChildAsync(dependency));
            return true;
        }));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        ScopedConfirmationFeature feature = await FeatureFactory.CreateAsync<ScopedConfirmationFeature>(provider);
        using CancellationTokenSource waiting = new();
        Task<CloseRequestResult> request = feature.RequestCloseAsync(waiting.Token);
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            waiting.Cancel();
            await Assert.That((await request.WaitAsync(Watchdog)).Kind).IsEqualTo(CloseRequestKind.WaitCanceled);
            await Assert.That(feature.IsClosed).IsFalse();
            CloseResult close = feature.Close();
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(resource!.DisposeCount).IsEqualTo(0);
            release.SetResult();
            await childEntered.Task.WaitAsync(Watchdog);
            await Assert.That(close.Ticket.Released.IsCompleted).IsFalse();
            releaseChild.SetResult();
            await Assert.That((await close.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            releaseChild.TrySetResult();
            await feature.Close().Ticket.Released.WaitAsync(Watchdog);
        }

        async Task ChildAsync(LifetimeResource dependency)
        {
            childEntered.SetResult();
            await releaseChild.Task;
            dependency.Use();
        }
    }

    /// <summary>确认回调可以合法编辑和新增成员，旧条件不能用于直接关闭新成员。</summary>
    /// <returns>门外确认合法结构修改验证任务。</returns>
    [Test]
    public async Task ConfirmationCanEditAndAddThenRevalidatesNewChild()
    {
        ConfirmationFeature? root = null;
        int calls = 0;
        int childCalls = 0;
        root = new(_ =>
        {
            if (++calls == 1)
            {
                root!.SetValue(7);
                root.Children.Add(new ConfirmationFeature(_ => { childCalls++; return ValueTask.FromResult(true); }));
            }

            return ValueTask.FromResult(true);
        });
        CloseRequestResult result = await root.RequestCloseAsync().WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(childCalls).IsEqualTo(1);
        await Assert.That(root.IsClosed).IsTrue();
        await root.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>确认上下文直接读取依赖自身退出的Released仍明确拒绝。</summary>
    /// <returns>确认自等待验证任务。</returns>
    [Test]
    public async Task ConfirmationCannotWaitForItsOwnRelease()
    {
        ConfirmationFeature? feature = null;
        bool protectedWait = false;
        feature = new(context =>
        {
            try { _ = feature!.Close().Ticket.Released; }
            catch (InvalidOperationException) { protectedWait = true; }
            return ValueTask.FromResult(true);
        });
        await feature.RequestCloseAsync().WaitAsync(Watchdog);
        await Assert.That(protectedWait).IsTrue();
        await feature.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>并发批准请求复用原票据，提交后的令牌取消不能伪报未关闭。</summary>
    /// <returns>并发提交唯一结果验证任务。</returns>
    [Test]
    public async Task ConcurrentApprovalSharesTicketAndLateCancellationKeepsClosedResult()
    {
        ConfirmationFeature feature = new(static _ => ValueTask.FromResult(true));
        using CancellationTokenSource cancellation = new();
        Task<CloseRequestResult> first = feature.RequestCloseAsync(cancellation.Token);
        Task<CloseRequestResult> second = feature.RequestCloseAsync();
        CloseRequestResult[] results = await Task.WhenAll(first, second).WaitAsync(Watchdog);
        cancellation.Cancel();
        await Assert.That(results.All(result => result.Kind == CloseRequestKind.Closed)).IsTrue();
        await Assert.That(ReferenceEquals(results[0].Close!.Ticket, results[1].Close!.Ticket)).IsTrue();
        await results[0].Close!.Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>值类型业务State的装箱和操作快照变化不使关闭条件失效。</summary>
    /// <returns>值类型状态版本验证任务。</returns>
    [Test]
    public async Task StructStateRuntimeChangesDoNotInvalidateConfirmation()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        StructConfirmationFeature feature = new(async _ => { calls++; entered.TrySetResult(); return await release.Task; });
        Task<CloseRequestResult> close = feature.RequestCloseAsync();
        await entered.Task.WaitAsync(Watchdog);
        await feature.NoChangeAsync();
        release.SetResult(true);
        CloseRequestResult result = await close.WaitAsync(Watchdog);
        await Assert.That(result.Kind).IsEqualTo(CloseRequestKind.Closed);
        await Assert.That(calls).IsEqualTo(1);
        await result.Close!.Ticket.Released.WaitAsync(Watchdog);
    }
}

internal sealed record ConfirmationMessage(int Value);
internal sealed record ConfirmationState
{
    [Input]
    public int Value { get; init; }
}

internal sealed partial class ConfirmationFeature : Feature<ConfirmationState>
{
    private readonly Func<CloseConfirmation<ConfirmationState>, ValueTask<bool>> confirm;
    internal ConfirmationFeature(Func<CloseConfirmation<ConfirmationState>, ValueTask<bool>> confirm) : base(new())
    {
        this.confirm = confirm;
        Port = CreateRequestPort<ConfirmationMessage, int>("Set", null, async (operation, message) =>
        {
            await operation.UpdateAsync(static (state, value) => state with { Value = value }, message.Value);
            return message.Value;
        });
    }
    internal RequestPort<ConfirmationMessage, int> Port { get; }
    protected override ValueTask<bool> ConfirmCloseAsync(CloseConfirmation<ConfirmationState> confirmation) => confirm(confirmation);
    [Operation]
    private ValueTask<int> NoChangeAsync(Operation<ConfirmationState> operation) => ValueTask.FromResult(operation.Snapshot.Value);
}

internal sealed class ConfirmationWork(Func<CloseConfirmation<ConfirmationState>, LifetimeResource, ValueTask<bool>> run)
{
    internal Func<CloseConfirmation<ConfirmationState>, LifetimeResource, ValueTask<bool>> Run { get; } = run;
}

internal sealed partial class ScopedConfirmationFeature(LifetimeResource resource, ConfirmationWork work) : Feature<ConfirmationState>(new())
{
    protected override ValueTask<bool> ConfirmCloseAsync(CloseConfirmation<ConfirmationState> confirmation)
        => work.Run(confirmation, resource);
}

internal readonly record struct StructConfirmationState(int Value);
internal sealed partial class StructConfirmationFeature(Func<CloseConfirmation<StructConfirmationState>, ValueTask<bool>> confirm)
    : Feature<StructConfirmationState>(new())
{
    protected override ValueTask<bool> ConfirmCloseAsync(CloseConfirmation<StructConfirmationState> confirmation) => confirm(confirmation);
    [Operation]
    private ValueTask<int> NoChangeAsync(Operation<StructConfirmationState> operation) => ValueTask.FromResult(operation.Snapshot.Value);
}
