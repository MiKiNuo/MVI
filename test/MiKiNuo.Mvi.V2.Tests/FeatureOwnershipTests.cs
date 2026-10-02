using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.V2.Tests;

/// <summary>通过公开所有权、端口与关闭票据验证动态独立实例树。</summary>
public sealed class FeatureOwnershipTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(15);

    /// <summary>业务相等但非同引用的实例均能接管、独立路由、关闭及回收，不调用用户相等性。</summary>
    /// <returns>实例引用身份集合验证任务。</returns>
    [Test]
    public async Task EqualBusinessFeaturesRemainDistinctOwnedInstances()
    {
        RequestDetailsFeature parent = new();
        EqualBusinessFeature first = new(7, true);
        EqualBusinessFeature second = new(7, true);
        Mediator mediator = new();
        try
        {
            parent.Children.Add(first).Wire(mediator, first.Port);
            parent.Children.Add(second).Wire(mediator, second.Port);
            await Assert.That(parent.Children.Active.Count).IsEqualTo(2);
            await Assert.That(first.InstanceId != second.InstanceId).IsTrue();
            await mediator.SendAsync(new LoadDetails(1), first.Port);
            await mediator.SendAsync(new LoadDetails(2), second.Port);
            await Assert.That(first.Snapshot.State.ObjectId).IsEqualTo(1);
            await Assert.That(second.Snapshot.State.ObjectId).IsEqualTo(2);
            await parent.Close().Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(first.IsClosed && second.IsClosed).IsTrue();
        }
        finally
        {
            await parent.Close().Ticket.Released.WaitAsync(Watchdog);
            await first.Close().Ticket.Released.WaitAsync(Watchdog);
            await second.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>父关闭先停止全部后代准入，再在树门外运行任何取消回调。</summary>
    /// <returns>父停止准入及锁边界验证任务。</returns>
    [Test]
    public async Task ParentStopsEveryChildBeforeCancellationCallbacksRun()
    {
        RequestDetailsFeature parent = new();
        RequestDetailsFeature peer = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> callback = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestDetailsFeature active = new(async (operation, _) =>
        {
            operation.CancellationToken.Register(() =>
            {
                bool stopped = Task.Run(() => parent.Children.Active.Count == 0 && parent.IsClosed && peer.IsClosed)
                    .WaitAsync(Watchdog).GetAwaiter().GetResult();
                callback.SetResult(stopped);
            });
            entered.SetResult();
            await release.Task;
            return "done";
        });
        parent.Children.Add(active);
        parent.Children.Add(peer);
        Task<OperationResult<string>> execution = active.LoadAsync(new(1));
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult close = parent.Close();
            await Assert.That(await callback.Task.WaitAsync(Watchdog)).IsTrue();
            release.SetResult();
            await execution.WaitAsync(Watchdog);
            await close.Ticket.Released.WaitAsync(Watchdog);
        }
        finally
        {
            release.TrySetResult();
            await execution.WaitAsync(Watchdog);
            await parent.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>纯规则中的生命周期或结构重入在尝试取得树锁之前拒绝，保留原快照。</summary>
    /// <returns>纯转换结构边界验证任务。</returns>
    [Test]
    public async Task PureRulesCannotReenterOwnershipOrClose()
    {
        PostFeature feature = new(1);
        FeatureOwnership ownership = feature.Children;
        RuntimeSnapshot<PostState> before = feature.Snapshot;
        await Assert.That(() => feature.HoldInput(() => feature.Close())).Throws<InvalidOperationException>();
        await Assert.That(() => feature.HoldInput(() => _ = ownership.Active)).Throws<InvalidOperationException>();
        await Assert.That(ReferenceEquals(feature.Snapshot, before)).IsTrue();
        await Assert.That(feature.IsClosed).IsFalse();
        await feature.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>同类型同业务对象实例身份独立，所有权必须单一、无环且不能接管已闭实例。</summary>
    /// <returns>所有权结构规则验证任务。</returns>
    [Test]
    public async Task OwnershipIsSingleAcyclicAndIndependentOfBusinessState()
    {
        RequestDetailsFeature root = new();
        RequestDetailsFeature other = new();
        RequestDetailsFeature first = new();
        RequestDetailsFeature second = new();
        FeatureMember one = root.Children.Add(first);
        root.Children.Add(second);
        await Assert.That(first.InstanceId != second.InstanceId).IsTrue();
        await Assert.That(first.Snapshot.State).IsEqualTo(second.Snapshot.State);
        await Assert.That(root.Children.Revision).IsEqualTo(2);
        await Assert.That(root.Children.Active.Count).IsEqualTo(2);
        await Assert.That(() => other.Children.Add(first)).Throws<InvalidOperationException>();
        await Assert.That(() => first.Children.Add(root)).Throws<InvalidOperationException>();
        await Assert.That(() => root.Children.Add(root)).Throws<InvalidOperationException>();
        await Assert.That(ReferenceEquals(one.Parent, root)).IsTrue();
        root.Close();
        await Assert.That(() => root.Children.Add(new RequestDetailsFeature())).Throws<FeatureClosedException>();
        await Assert.That(() => other.Children.Add(second)).Throws<FeatureClosedException>();
        await root.Close().Ticket.Released.WaitAsync(Watchdog);
        await other.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>子直接关闭与成员移除统一更新活动成员和路由，剩余唯一候选恢复。</summary>
    /// <returns>动态路由恢复验证任务。</returns>
    [Test]
    public async Task ChildCloseAndRemoveRetireOwnedRoutesAndRestoreUniqueSelection()
    {
        RequestDetailsFeature root = new();
        RequestDetailsFeature first = new();
        RequestDetailsFeature second = new();
        Mediator mediator = new();
        FeatureMember one = root.Children.Add(first);
        FeatureMember two = root.Children.Add(second);
        one.Wire(mediator, first.Load);
        two.Wire(mediator, second.Load);
        await Assert.That(() => one.Wire(mediator, second.Load)).Throws<InvalidOperationException>();
        await Assert.That((await mediator.SendAsync<LoadDetails, string>(new(7))).Kind).IsEqualTo(RequestResultKind.AmbiguousTarget);
        await mediator.SendAsync(new LoadDetails(7), first.Load);
        await mediator.SendAsync(new LoadDetails(7), second.Load);
        CloseResult firstClosed = first.Close();
        await Assert.That(ReferenceEquals(one.Remove(), firstClosed)).IsTrue();
        await Assert.That(root.Children.Active.Count).IsEqualTo(1);
        await Assert.That(root.Children.Revision).IsEqualTo(3);
        await Assert.That((await mediator.SendAsync(new LoadDetails(8), first.Load)).Kind).IsEqualTo(RequestResultKind.TargetUnavailable);
        RequestResult<string> remaining = await mediator.SendAsync<LoadDetails, string>(new(9));
        await Assert.That(remaining.OperationResult!.Value).IsEqualTo("details 9");
        await Assert.That(second.Snapshot.State.ObjectId).IsEqualTo(9);
        two.Remove();
        await Assert.That((await mediator.SendAsync<LoadDetails, string>(new(1))).Kind).IsEqualTo(RequestResultKind.MissingTarget);
        await root.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>已从活动树移除但忽略取消的 scoped 子执行仍被父票据跟踪到真正退出。</summary>
    /// <returns>退休资源与独立 Scope 验证任务。</returns>
    [Test]
    public async Task ParentWaitsForRetiredScopedChildAndKeepsItsOwnScopeAlive()
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider parentProvider = LifetimeServices.Create(new());
        await using ServiceProvider childProvider = LifetimeServices.Create(new(async (_, value, resource) =>
        {
            entered.SetResult();
            await release.Task;
            resource.Use();
            return value;
        }));
        LifecycleFeature parent = await FeatureFactory.CreateAsync<LifecycleFeature>(parentProvider);
        LifecycleFeature child = await FeatureFactory.CreateAsync<LifecycleFeature>(childProvider);
        FeatureMember member = parent.Children.Add(child);
        Task<OperationResult<int>> operation = child.RejectAsync();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            CloseResult retired = member.Remove();
            await Assert.That(parent.Children.Active.Count).IsEqualTo(0);
            await Assert.That(parent.Children.Retired.Count).IsEqualTo(1);
            CloseResult closed = parent.Close();
            await Assert.That(retired.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(closed.Ticket.Released.IsCompleted).IsFalse();
            await Assert.That(parent.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(child.Resource.DisposeCount).IsEqualTo(0);
            await Assert.That(ReferenceEquals(parent.Resource, child.Resource)).IsFalse();
            release.SetResult();
            await operation.WaitAsync(Watchdog);
            await Assert.That((await closed.Ticket.Released.WaitAsync(Watchdog)).Succeeded).IsTrue();
            await Assert.That(parent.Resource.DisposeCount).IsEqualTo(1);
            await Assert.That(child.Resource.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            release.TrySetResult();
            await operation.WaitAsync(Watchdog);
            await parent.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    /// <summary>退休子释放失败保留到父后续关闭，并且父自己的范围仍完成唯一清理。</summary>
    /// <returns>子释放失败传播验证任务。</returns>
    [Test]
    public async Task RetiredReleaseFailureIsNotLostBeforeParentClose()
    {
        InvalidOperationException failure = new("retired child disposal fault");
        await using ServiceProvider parentProvider = LifetimeServices.Create(new());
        await using ServiceProvider childProvider = LifetimeServices.Create(new(), _ => new(() => ValueTask.FromException(failure)));
        LifecycleFeature parent = await FeatureFactory.CreateAsync<LifecycleFeature>(parentProvider);
        LifecycleFeature child = await FeatureFactory.CreateAsync<LifecycleFeature>(childProvider);
        FeatureMember member = parent.Children.Add(child);
        ReleaseResult releasedChild = await member.Remove().Ticket.Released.WaitAsync(Watchdog);
        await Assert.That(releasedChild.Succeeded).IsFalse();
        ReleaseResult releasedParent = await parent.Close().Ticket.Released.WaitAsync(Watchdog);
        await Assert.That(releasedParent.Succeeded).IsFalse();
        await Assert.That(releasedParent.Exception is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions.Contains(failure) : ReferenceEquals(releasedParent.Exception, failure)).IsTrue();
        await Assert.That(parent.Resource.DisposeCount).IsEqualTo(1);
    }

    /// <summary>嵌套树一次关闭停用全部后代，子操作不能等待依赖自己退出的父释放。</summary>
    /// <returns>嵌套关闭与父等待保护验证任务。</returns>
    [Test]
    public async Task NestedChildCannotWaitForDependentAncestorRelease()
    {
        RequestDetailsFeature root = new();
        RequestDetailsFeature middle = new();
        bool protectedWait = false;
        RequestDetailsFeature leaf = new((_, _) =>
        {
            CloseResult close = root.Close();
            try { _ = close.Ticket.Released; }
            catch (InvalidOperationException) { protectedWait = true; }
            return ValueTask.FromResult("closed");
        });
        root.Children.Add(middle);
        middle.Children.Add(leaf);
        await leaf.LoadAsync(new(1)).WaitAsync(Watchdog);
        await Assert.That(protectedWait).IsTrue();
        await Assert.That(root.IsClosed && middle.IsClosed && leaf.IsClosed).IsTrue();
        await root.Close().Ticket.Released.WaitAsync(Watchdog);
    }

    /// <summary>子直接关闭、Remove 和父关闭并发时保留相同票据，不漏快速释放。</summary>
    /// <returns>并发重复关闭验证任务。</returns>
    [Test]
    public async Task ConcurrentChildRemovalAndParentCloseReuseTickets()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            RequestDetailsFeature parent = new();
            RequestDetailsFeature child = new();
            FeatureMember member = parent.Children.Add(child);
            TaskCompletionSource race = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<CloseResult> direct = Task.Run(async () => { await race.Task; return child.Close(); });
            Task<CloseResult> remove = Task.Run(async () => { await race.Task; return member.Remove(); });
            Task<CloseResult> close = Task.Run(async () => { await race.Task; return parent.Close(); });
            race.SetResult();
            CloseResult[] results = await Task.WhenAll(direct, remove, close).WaitAsync(Watchdog);
            await Assert.That(ReferenceEquals(results[0], results[1])).IsTrue();
            await results[2].Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(parent.Children.Active.Count).IsEqualTo(0);
        }
    }

    /// <summary>新增与父关闭竞争时只允许成功接管后随树关闭或明确拒绝，不遗留活动子实例。</summary>
    /// <returns>新增与树关闭排序验证任务。</returns>
    [Test]
    public async Task AddAndCloseRaceCannotLeaveAnOwnedChildOpen()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            RequestDetailsFeature parent = new();
            RequestDetailsFeature child = new();
            TaskCompletionSource race = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> add = Task.Run(async () =>
            {
                await race.Task;
                try { parent.Children.Add(child); return true; }
                catch (FeatureClosedException) { return false; }
            });
            Task<CloseResult> close = Task.Run(async () => { await race.Task; return parent.Close(); });
            race.SetResult();
            bool owned = await add.WaitAsync(Watchdog);
            await (await close.WaitAsync(Watchdog)).Ticket.Released.WaitAsync(Watchdog);
            await Assert.That(parent.Children.Active.Count).IsEqualTo(0);
            if (owned) await Assert.That(child.IsClosed).IsTrue();
            await child.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }
}

internal sealed partial class EqualBusinessFeature(int businessId) : Feature<RequestDetailsState>(new())
{
    internal RequestPort<LoadDetails, string> Port { get; } = null!;
    private readonly int businessId = businessId;

    public EqualBusinessFeature(int businessId, bool initialize = true) : this(businessId)
    {
        _ = initialize;
        Port = CreateRequestPort<LoadDetails, string>("Load", null, async (operation, request) =>
        {
            await operation.UpdateAsync(static (state, value) => state with { ObjectId = value }, request.ObjectId);
            return request.ObjectId.ToString();
        });
    }

    public override bool Equals(object? obj) => obj is EqualBusinessFeature other && businessId == other.businessId;
    public override int GetHashCode() => businessId;
}
