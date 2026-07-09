using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示床位筛选意图与规约器的回归测试。
/// 验证 SetBedFilter 仅 BedOverview 卡片生效；其他卡片被处理器静默忽略。
/// </summary>
public sealed class CardBedFilterTests
{
    /// <summary>
    /// BedOverview 卡片派发 SetBedFilter(Open) 后：CurrentBedFilter 变 Open，FilteredBedCount 等于 BedCatalog 中 Open 状态的床位数。
    /// </summary>
    [Test]
    public async Task SetBedFilter_Open_OnBedOverview_UpdatesFilterAndCountAsync()
    {
        using MviStore<CardState, CardIntent, CardEffect> store = CreateStore(PageKey.BedOverview);

        // sanity: 初始是 All
        await Assert.That(store.CurrentState.CurrentBedFilter).IsEqualTo(BedFilter.All);
        await Assert.That(store.CurrentState.FilteredBedCount).IsEqualTo(BedCatalog.TotalCount);

        await store.DispatchAsync(new CardIntent.SetBedFilter(BedFilter.Open));

        await Assert.That(store.CurrentState.CurrentBedFilter).IsEqualTo(BedFilter.Open);
        await Assert.That(store.CurrentState.FilteredBedCount).IsEqualTo(BedCatalog.Count(BedFilter.Open));
        await Assert.That(store.CurrentState.PageKey).IsEqualTo(PageKey.BedOverview);
        await Assert.That(store.CurrentState.ActionLog).Contains("床位筛选已切换为");
    }

    /// <summary>
    /// SetBedFilter 派发到非 BedOverview 卡片时处理器静默忽略：state 不变，FilteredBedCount 不变。
    /// </summary>
    [Test]
    public async Task SetBedFilter_IgnoredOnNonBedOverviewCardAsync()
    {
        CardState initial = NewState(PageKey.NursingTaskBoard);
        using MviStore<CardState, CardIntent, CardEffect> store = CreateStore(initial);

        await store.DispatchAsync(new CardIntent.SetBedFilter(BedFilter.Occupied));

        // 处理器短路：state 应等于传入的实例
        await Assert.That(store.CurrentState).IsEqualTo(initial);
    }

    /// <summary>
    /// SetBedFilter 派发到 BedOverview 卡片但 Filter 与当前相同：处理器返回空变更（无变化）。
    /// </summary>
    [Test]
    public async Task SetBedFilter_SameValue_IsNoopAsync()
    {
        using MviStore<CardState, CardIntent, CardEffect> store = CreateStore(PageKey.BedOverview);
        CardState initial = store.CurrentState;
        // initial.CurrentBedFilter = All

        await store.DispatchAsync(new CardIntent.SetBedFilter(BedFilter.All));

        await Assert.That(store.CurrentState).IsEqualTo(initial);
    }

    /// <summary>
    /// 切换为 Occupied 后 FilteredBedCount 反映 Occupied 床位数；与 BedCatalog.Count 一致。
    /// </summary>
    [Test]
    public async Task SetBedFilter_Occupied_CountMatchesCatalogAsync()
    {
        using MviStore<CardState, CardIntent, CardEffect> store = CreateStore(PageKey.BedOverview);

        await store.DispatchAsync(new CardIntent.SetBedFilter(BedFilter.Occupied));

        await Assert.That(store.CurrentState.CurrentBedFilter).IsEqualTo(BedFilter.Occupied);
        await Assert.That(store.CurrentState.FilteredBedCount).IsEqualTo(BedCatalog.Count(BedFilter.Occupied));
        // 数值应为正（演示数据刻意生成了占用床位）
        await Assert.That(store.CurrentState.FilteredBedCount).IsGreaterThan(0);
    }

    private static CardState NewState(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)!;
        return CardState.FromDefinition(definition);
    }

    private static MviStore<CardState, CardIntent, CardEffect> CreateStore(PageKey key)
    {
        CardState initial = NewState(key);
        return CreateStore(initial);
    }

    private static MviStore<CardState, CardIntent, CardEffect> CreateStore(CardState initial)
    {
        return new MviStore<CardState, CardIntent, CardEffect>(
            initial,
            new CardIntentHandler(),
            new CardReducer(DashboardCardRegistry.All),
            new NoopEffectDispatcher<CardEffect>());
    }
}
