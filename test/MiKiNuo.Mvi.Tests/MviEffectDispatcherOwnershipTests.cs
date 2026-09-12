using MiKiNuo.Mvi.Application.MVI.Store;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证回流副作用派发器只能归属于一个存储实例。</summary>
public sealed class MviEffectDispatcherOwnershipTests
{
    /// <summary>拒绝第二个存储接线，并保留首个存储的回流目标。</summary>
    [Test]
    public async Task ReusedDispatcher_ShouldRejectSecondStoreAndPreserveFeedbackAsync()
    {
        FeedbackEffectDispatcher dispatcher = new();
        await using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> first =
            new(StoreTestState.Initial, new StoreTestReducer(), dispatcher);

        await Assert.That(() =>
        {
            using MviStore<StoreTestState, StoreTestIntent, StoreTestEffect> second =
                new(StoreTestState.Initial, new StoreTestReducer(), dispatcher);
        }).Throws<InvalidOperationException>();

        await first.DispatchAsync(new StoreTestIntent.TriggerFeedback());

        await Assert.That(first.CurrentState.Count).IsEqualTo(1);
    }
}
