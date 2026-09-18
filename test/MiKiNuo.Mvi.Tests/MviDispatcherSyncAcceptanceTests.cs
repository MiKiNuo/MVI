using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 验证通知接纳通道：分发器经 <c>TryAcceptIntent</c> 同步接纳本地意图进入 Store 通知队列。
/// </summary>
public sealed class MviDispatcherSyncAcceptanceTests
{
    /// <summary>分发器未附加到 Store 时同步接纳抛出异常。</summary>
    [Test]
    public async Task TryAcceptIntent_WhenNotAttached_Should_ThrowAsync()
    {
        SyncAcceptEffectDispatcher dispatcher = new();

        await Assert.That(() => dispatcher.Accept(new StoreTestIntent.Increment()))
            .Throws<InvalidOperationException>();
    }

    /// <summary>分发器附加后同步接纳成功，意图经通知队列进入规约管线。</summary>
    [Test]
    public async Task TryAcceptIntent_WhenAttached_Should_PostIntoNotificationQueueAsync()
    {
        SyncAcceptEffectDispatcher dispatcher = new();
        await using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial,
            new StoreTestReducer(),
            dispatcher);

        bool accepted = dispatcher.Accept(new StoreTestIntent.Increment());

        await Assert.That(accepted).IsTrue();
        await WaitForConditionAsync(() => store.CurrentState.Count == 1);
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }

    /// <summary>Store 关闭后同步接纳明确拒绝（返回假而非抛出）。</summary>
    [Test]
    public async Task TryAcceptIntent_WhenStoreDisposed_Should_ReturnFalseAsync()
    {
        SyncAcceptEffectDispatcher dispatcher = new();
        MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> store = new(
            StoreTestState.Initial,
            new StoreTestReducer(),
            dispatcher);
        await store.DisposeAsync();

        bool accepted = dispatcher.Accept(new StoreTestIntent.Increment());

        await Assert.That(accepted).IsFalse();
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }
    }
}

/// <summary>
/// 表示同步接纳测试分发器：暴露受保护的 TryAcceptIntent 供测试驱动。
/// </summary>
public sealed partial class SyncAcceptEffectDispatcher
    : MviEffectDispatcherBase<StoreTestIntent, StoreTestEffect>
{
    /// <summary>
    /// 同步接纳本地意图。
    /// </summary>
    /// <param name="intent">本地意图。</param>
    /// <returns>接纳成功时为真。</returns>
    public bool Accept(StoreTestIntent intent) => TryAcceptIntent(intent);
}
