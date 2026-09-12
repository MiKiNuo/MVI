namespace MiKiNuo.Mvi.Tests;

using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>验证组合范围内的请求与通知。</summary>
public sealed class MviCompositionMediatorTests
{
    /// <summary>端点注销必须等待已准入的同步接纳回调退出。</summary>
    [Test]
    public async Task EndpointCloseWaitsForAdmittedNotificationAsync()
    {
        using MviCompositionScope scope = new();
        using MviMediatorEndpoint origin = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint target = scope.CreateEndpoint(Guid.NewGuid());
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        using ManualResetEventSlim closed = new();
        using IDisposable subscription = scope.Subscribe<Changed>(target.InstanceId, _ => { entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); });
        Task publishing = Task.Run(async () => await origin.PublishAsync(new Changed()));
        await Task.Run(() => entered.Wait(TimeSpan.FromSeconds(5)));
        Task closing = Task.Run(() => { target.Dispose(); closed.Set(); });
        bool premature = await Task.Run(() => closed.Wait(TimeSpan.FromMilliseconds(100)));
        release.Set();
        await Task.WhenAll(publishing, closing);
        await Assert.That(premature).IsFalse();
    }

    /// <summary>投递期间取消后其余订阅明确标记为未尝试。</summary>
    [Test]
    public async Task CancellationStopsRemainingNotificationsAsync()
    {
        using MviCompositionScope scope = new();
        using MviMediatorEndpoint origin = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint first = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint second = scope.CreateEndpoint(Guid.NewGuid());
        using CancellationTokenSource cancellation = new();
        int received = 0;
        using IDisposable a = scope.Subscribe<Changed>(first.InstanceId, _ => cancellation.Cancel());
        using IDisposable b = scope.Subscribe<Changed>(second.InstanceId, _ => received++);
        MviNotificationReport report = await origin.PublishAsync(new Changed(), cancellation.Token);
        await Assert.That(received).IsEqualTo(0);
        await Assert.That(report.Deliveries[1].Status).IsEqualTo(MviNotificationDeliveryStatus.NotAttempted);
    }

    /// <summary>表示已发生的变化。</summary>
    private sealed record Changed : IMviNotification;
    /// <summary>同类型请求按来源绑定到明确实例。</summary>
    [Test]
    public async Task RequestsFollowInstanceBindingsAsync()
    {
        using MviCompositionScope scope = new();
        using MviMediatorEndpoint source = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint first = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint second = scope.CreateEndpoint(Guid.NewGuid());
        scope.Register<Query, string>(first.InstanceId, (_, _) => ValueTask.FromResult("一"));
        scope.Register<Query, string>(second.InstanceId, (_, _) => ValueTask.FromResult("二"));
        scope.Bind<Query>(source.InstanceId, second.InstanceId);
        await Assert.That(await source.SendAsync(new Query())).IsEqualTo("二");
    }

    /// <summary>关闭目标会结束忽略取消的处理器等待。</summary>
    [Test]
    public async Task ClosingTargetCancelsPendingRequestAsync()
    {
        using MviCompositionScope scope = new();
        using MviMediatorEndpoint source = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint target = scope.CreateEndpoint(Guid.NewGuid());
        TaskCompletionSource<string> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Register<Query, string>(target.InstanceId, (_, _) => new ValueTask<string>(response.Task));
        scope.Bind<Query>(source.InstanceId, target.InstanceId);
        Task<string> pending = source.SendAsync(new Query()).AsTask();
        target.Dispose();
        try
        {
            await Assert.That(async () => await pending.WaitAsync(TimeSpan.FromSeconds(2))).Throws<OperationCanceledException>();
        }
        finally
        {
            response.TrySetResult("迟到结果");
        }
    }

    /// <summary>通知只投递本范围非自身订阅者并隔离接纳失败。</summary>
    [Test]
    public async Task NotificationsAreScopedAndIsolateFailuresAsync()
    {
        using MviCompositionScope scope = new();
        using MviCompositionScope other = new();
        using MviMediatorEndpoint source = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint failing = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint target = scope.CreateEndpoint(Guid.NewGuid());
        using MviMediatorEndpoint outsider = other.CreateEndpoint(Guid.NewGuid());
        int accepted = 0;
        using IDisposable self = scope.Subscribe<Notice>(source.InstanceId, _ => throw new Exception("自身不得接收"));
        using IDisposable foreign = other.Subscribe<Notice>(outsider.InstanceId, _ => throw new Exception("范围不得泄漏"));
        using IDisposable bad = scope.Subscribe<Notice>(failing.InstanceId, _ => throw new InvalidOperationException("容量不足"));
        using IDisposable good = scope.Subscribe<Notice>(target.InstanceId, _ => accepted++);
        MviNotificationReport report = await source.PublishAsync(new Notice());
        await Assert.That(accepted).IsEqualTo(1);
        await Assert.That(report.Deliveries.Count).IsEqualTo(2);
        await Assert.That(report.Deliveries[0].Status).IsEqualTo(MviNotificationDeliveryStatus.Failed);
        await Assert.That(report.Deliveries[0].Exception).IsTypeOf<InvalidOperationException>();
        await Assert.That(report.Deliveries[1].TargetInstanceId).IsEqualTo(target.InstanceId);
        await Assert.That(report.Deliveries[1].Status).IsEqualTo(MviNotificationDeliveryStatus.Accepted);
    }

    /// <summary>通知契约。</summary>
    private sealed record Notice : IMviNotification;

    /// <summary>查询契约。</summary>
    private sealed record Query : IMviRequest<string>;
}
