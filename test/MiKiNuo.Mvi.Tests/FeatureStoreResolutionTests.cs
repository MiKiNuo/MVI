using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Tests.Composition;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 Feature Store 装配的端到端验证。
/// 通过测试程序集内生成的 GeneratedMviContainer 真实解析 Counter Feature 的 Store。
/// </summary>
public sealed class FeatureStoreResolutionTests
{
    /// <summary>
    /// 验证生成的容器可以解析 Feature Store 并完成一次真实派发。
    /// </summary>
    [Test]
    public async Task Resolve_Should_ReturnWorkingFeatureStoreAsync()
    {
        GeneratedMviContainer container = new();
        IMviStore<CounterState, CounterIntent, UnitEffect> store =
            container.Resolve<IMviStore<CounterState, CounterIntent, UnitEffect>>();
        await store.DispatchAsync(new CounterIntent.Increment());
        await Assert.That(store.CurrentState.Count).IsEqualTo(1);
    }

    /// <summary>
    /// 验证 Feature Store 解析为单例。
    /// </summary>
    [Test]
    public async Task Resolve_Should_ReturnSingletonFeatureStoreAsync()
    {
        GeneratedMviContainer container = new();
        IMviStore<CounterState, CounterIntent, UnitEffect> first =
            container.Resolve<IMviStore<CounterState, CounterIntent, UnitEffect>>();
        IMviStore<CounterState, CounterIntent, UnitEffect> second =
            container.Resolve<IMviStore<CounterState, CounterIntent, UnitEffect>>();
        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }
}
