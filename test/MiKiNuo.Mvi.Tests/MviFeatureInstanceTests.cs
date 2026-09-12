using MiKiNuo.Mvi.Application.MVI.Composition;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

#pragma warning disable CA2000 // 测试把资源所有权交给实例，由实例验证释放。

/// <summary>验证实例资源树的所有权与关闭协议。</summary>
public sealed class MviFeatureInstanceTests
{
    /// <summary>父级关闭必须保留已关闭子级的提交失败，不能误报整体成功。</summary>
    [Test]
    public async Task ParentClosePreservesChildCommitFailureAsync()
    {
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", []);
        MviFeatureInstance<string> child = new(Guid.NewGuid(), "子", []);
        await parent.Own(child);
        await Assert.That(async () => await child.TryCloseAsync(
            (_, _) => ValueTask.FromResult<long?>(0), (_, _, _) => ValueTask.FromResult(true),
            (_, _) => ValueTask.CompletedTask, (_, _) => throw new InvalidOperationException("子级提交失败")))
            .Throws<AggregateException>();
        await Assert.That(async () => await parent.TryCloseAsync(
            (_, _) => ValueTask.FromResult<long?>(0), (_, _, _) => ValueTask.FromResult(true),
            (_, _) => ValueTask.CompletedTask, (_, _) => ValueTask.CompletedTask))
            .Throws<AggregateException>();
    }

    /// <summary>子级已独立关闭后，父级仅确认仍活动的节点并正常回收。</summary>
    [Test]
    public async Task ParentCanCloseAfterChildClosedAsync()
    {
        List<string> released = [];
        List<Guid> confirmed = [];
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", [new Resource("父", released)]);
        MviFeatureInstance<string> child = new(Guid.NewGuid(), "子", [new Resource("子", released)]);
        await parent.Own(child);
        await child.DisposeAsync();
        try
        {
            bool closed = await parent.TryCloseAsync(
                (id, _) => { confirmed.Add(id); return ValueTask.FromResult<long?>(0); },
                (_, _, _) => ValueTask.FromResult(true),
                (_, _) => ValueTask.CompletedTask, (_, _) => ValueTask.CompletedTask);
            await Assert.That(closed).IsTrue();
            await Assert.That(confirmed.Count).IsEqualTo(1);
            await Assert.That(confirmed[0]).IsEqualTo(parent.Id);
            await Assert.That(string.Join(",", released)).IsEqualTo("子,父");
        }
        finally { await parent.DisposeAsync(); }
    }

    /// <summary>重复关闭保留首次提交控制请求的失败结果。</summary>
    [Test]
    public async Task RepeatedClosePreservesCommitFailureAsync()
    {
        MviFeatureInstance<string> instance = new(Guid.NewGuid(), "视图", []);
        ValueTask<bool> Close() => instance.TryCloseAsync((_, _) => ValueTask.FromResult<long?>(0),
            (_, _, _) => ValueTask.FromResult(true), (_, _) => ValueTask.CompletedTask,
            (_, _) => throw new InvalidOperationException("提交失败"));
        await Assert.That(async () => await Close()).Throws<AggregateException>();
        await Assert.That(async () => await Close()).Throws<AggregateException>();
    }
    /// <summary>父级后接管的资源仍须等待子实例释放完成，重复关闭等待同一结果。</summary>
    [Test]
    public async Task ParentResourcesAndRepeatedCloseWaitForChildrenAsync()
    {
        List<string> released = [];
        DelayedResource delayed = new();
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", []);
        MviFeatureInstance<string> child = new(Guid.NewGuid(), "子", [delayed]);
        await parent.Own(child);
        await parent.Own(new Resource("父资源", released));
        Task first = parent.DisposeAsync().AsTask();
        await delayed.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<bool> second = parent.TryCloseAsync((_, _) => ValueTask.FromResult<long?>(0),
            (_, _, _) => ValueTask.FromResult(true), (_, _) => ValueTask.CompletedTask,
            (_, _) => ValueTask.CompletedTask).AsTask();
        bool premature = second.IsCompleted;
        bool parentReleased = released.Count != 0;
        delayed.Release.TrySetResult();
        await first;
        await second;
        await Assert.That(premature).IsFalse();
        await Assert.That(parentReleased).IsFalse();
    }

    /// <summary>可控制完成时机的资源。</summary>
    private sealed class DelayedResource : IAsyncDisposable
    {
        /// <summary>释放进入信号。</summary>
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>允许释放信号。</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>等待测试允许释放。</summary>
        /// <returns>释放任务。</returns>
        public async ValueTask DisposeAsync() { Started.TrySetResult(); await Release.Task; }
    }
    /// <summary>释放按逆序执行，并优先等待异步释放。</summary>
    [Test]
    public async Task DisposeReleasesResourcesInReverseOrderAsync()
    {
        List<string> released = [];
        MviFeatureInstance<string> instance = new(Guid.NewGuid(), "视图", [new Resource("一", released), new Resource("二", released)]);
        await instance.DisposeAsync();
        await instance.DisposeAsync();
        await Assert.That(string.Join(",", released)).IsEqualTo("二,一");
        await Assert.That(instance.Lifetime.IsCancellationRequested).IsTrue();
    }

    /// <summary>挂载子树并回收关闭后迟到的孤儿。</summary>
    [Test]
    public async Task OwnClosesChildrenAndRejectsLateResourcesAsync()
    {
        List<string> released = [];
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", [new Resource("父", released)]);
        MviFeatureInstance<int> child = new(Guid.NewGuid(), 1, [new Resource("子", released)]);
        await Assert.That(await parent.Own(child)).IsTrue();
        await parent.DisposeAsync();
        await Assert.That(await parent.Own(new Resource("孤儿", released))).IsFalse();
        await Assert.That(child.IsClosed).IsTrue();
        await Assert.That(string.Join(",", released)).IsEqualTo("子,父,孤儿");
    }

    /// <summary>版本变化撤回整个子树，允许随后重新确认并由叶子提交。</summary>
    [Test]
    public async Task VersionMismatchResumesWholeTreeBeforeRetryAsync()
    {
        List<string> released = [];
        List<Guid> resumed = [];
        List<Guid> committed = [];
        MviFeatureInstance<string> parent = new(Guid.NewGuid(), "父", [new Resource("父", released)]);
        MviFeatureInstance<int> child = new(Guid.NewGuid(), 1, [new Resource("子", released)]);
        await parent.Own(child);
        bool accepted = await parent.TryCloseAsync(
            (id, token) => ValueTask.FromResult<long?>(7),
            (id, version, token) => ValueTask.FromResult(id != child.Id),
            (id, version) => { resumed.Add(id); return ValueTask.CompletedTask; },
            (id, version) => { committed.Add(id); return ValueTask.CompletedTask; });
        await Assert.That(accepted).IsFalse();
        await Assert.That(resumed.Count).IsEqualTo(2);
        await Assert.That(released.Count).IsEqualTo(0);
        await Assert.That(parent.Lifetime.IsCancellationRequested).IsFalse();
        accepted = await parent.TryCloseAsync(
            (id, token) => ValueTask.FromResult<long?>(8),
            (id, version, token) => ValueTask.FromResult(version == 8),
            (id, version) => ValueTask.CompletedTask,
            (id, version) => { committed.Add(id); return ValueTask.CompletedTask; });
        await Assert.That(accepted).IsTrue();
        await Assert.That(string.Join(",", committed)).IsEqualTo($"{child.Id},{parent.Id}");
        await Assert.That(string.Join(",", released)).IsEqualTo("子,父");
    }

    /// <summary>记录资源释放方式。</summary>
    /// <param name="name">资源名称。</param>
    /// <param name="released">释放记录。</param>
    private sealed class Resource(string name, List<string> released) : IDisposable, IAsyncDisposable
    {
        /// <summary>记录错误的同步释放。</summary>
        public void Dispose() => released.Add("错误的同步释放");
        /// <summary>记录异步释放。</summary>
        /// <returns>已完成的释放任务。</returns>
        public ValueTask DisposeAsync()
        {
            released.Add(name);
            return ValueTask.CompletedTask;
        }
    }
}
