using MiKiNuo.Mvi.Application.MVI.Composition;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

#pragma warning disable CA2000 // 测试把资源所有权交给实例，由实例验证释放。

/// <summary>验证实例资源树的所有权与释放。</summary>
public sealed class MviFeatureInstanceTests
{
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
        Task second = parent.DisposeAsync().AsTask();
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

    /// <summary>显式声明的停止动作在释放时先于资源回收调用。</summary>
    [Test]
    public async Task DisposeInvokesDeclaredStopsBeforeResourcesAsync()
    {
        List<string> calls = [];
        MviFeatureInstance<string> instance = new(
            Guid.NewGuid(), "视图",
            [new Resource("资源", calls)],
            [() => calls.Add("停止")]);
        await instance.DisposeAsync();
        await Assert.That(string.Join(",", calls)).IsEqualTo("停止,资源");
    }

    /// <summary>未显式声明停止动作的资源不被反查调用任何停止方法。</summary>
    [Test]
    public async Task DisposeDoesNotProbeUndeclaredStopsAsync()
    {
        StoppableResource resource = new();
        MviFeatureInstance<string> instance = new(Guid.NewGuid(), "视图", [resource]);
        await instance.DisposeAsync();
        await Assert.That(resource.StopCalled).IsFalse();
        await Assert.That(resource.Disposed).IsTrue();
    }

    /// <summary>Own 接管的资源可随所有权一并声明停止动作。</summary>
    [Test]
    public async Task OwnAcceptsStopActionForLateResourceAsync()
    {
        List<string> calls = [];
        MviFeatureInstance<string> instance = new(Guid.NewGuid(), "视图", [new Resource("初始", calls)]);
        await Assert.That(await instance.Own(new Resource("后挂", calls), () => calls.Add("后挂停止"))).IsTrue();
        await instance.DisposeAsync();
        await Assert.That(string.Join(",", calls)).IsEqualTo("后挂停止,后挂,初始");
    }

    /// <summary>记录停止与释放的探针资源。</summary>
    private sealed class StoppableResource : IDisposable
    {
        /// <summary>停止是否被调用。</summary>
        public bool StopCalled { get; private set; }

        /// <summary>是否已释放。</summary>
        public bool Disposed { get; private set; }

        /// <summary>探针停止方法，不应被实例反查调用。</summary>
        public void Stop() => StopCalled = true;

        /// <summary>记录释放。</summary>
        public void Dispose() => Disposed = true;
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
