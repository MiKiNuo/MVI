using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示基准项目 R3 状态流与事件绑定场景的冒烟测试：
/// 验证状态发布订阅与事件到 Intent 派发全链在测量前真实可用。
/// </summary>
public sealed class BenchmarkEventStreamScenarioSmokeTests
{
    /// <summary>
    /// 验证发布 1000 个状态时全部订阅者各自收到 1000 次通知且终态计数正确。
    /// <para>
    /// R3 ReactiveProperty 采用 BehaviorSubject 语义：订阅时先重放当前值一次
    /// （与 ViewModel 构造即收到当前状态的行为一致），
    /// 因此计数订阅者在 1000 次发布后共收到 1001 次通知。
    /// </para>
    /// </summary>
    [Test]
    public async Task R3StateStreamScenario_Should_NotifyAllSubscribers_WhenPublishingAsync()
    {
        using R3StateStreamScenario scenario = new();
        int firstReceived = 0;
        int secondReceived = 0;
        int thirdReceived = 0;

        scenario.Subscribe(static _ => { });
        scenario.Subscribe(_ => firstReceived++);
        scenario.Subscribe(_ => secondReceived++);
        scenario.Subscribe(_ => thirdReceived++);

        scenario.Publish(1000);

        await Assert.That(scenario.SubscriberCount).IsEqualTo(4);
        await Assert.That(scenario.PublishedCount).IsEqualTo(1000);
        await Assert.That(firstReceived).IsEqualTo(1001);
        await Assert.That(secondReceived).IsEqualTo(1001);
        await Assert.That(thirdReceived).IsEqualTo(1001);
        await Assert.That(scenario.CurrentState.Counter).IsEqualTo(1000);
    }

    /// <summary>
    /// 验证事件绑定场景把 100 个事件映射为 Intent 派发到 Store，计数精确对账。
    /// </summary>
    [Test]
    public async Task EventBindingScenario_Should_RouteEvents_ToStoreAsIntentsAsync()
    {
        using EventBindingScenario scenario = new();

        scenario.RaiseEvents(100);
        await scenario.WaitUntilCounterAsync(100);

        await Assert.That(scenario.RaisedEventCount).IsEqualTo(100);
        await Assert.That(scenario.CurrentCounter).IsEqualTo(100);
    }

    /// <summary>
    /// 验证事件绑定场景在零事件时不产生任何派发。
    /// </summary>
    [Test]
    public async Task EventBindingScenario_Should_NotDispatch_WhenNoEventsRaisedAsync()
    {
        using EventBindingScenario scenario = new();

        await Assert.That(scenario.RaisedEventCount).IsEqualTo(0);
        await Assert.That(scenario.CurrentCounter).IsEqualTo(0);
    }
}
