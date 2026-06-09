using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示床位筛选意图与归约器的回归测试。
/// 验证 SetBedFilter 仅 BedOverview 卡片生效；其他卡片被 reducer 静默忽略。
/// </summary>
public sealed class CardBedFilterTests
{
    /// <summary>
    /// BedOverview 卡片派发 SetBedFilter(Open) 后：CurrentBedFilter 变 Open，FilteredBedCount 等于 BedCatalog 中 Open 状态的床位数。
    /// </summary>
    [Test]
    public async Task SetBedFilter_Open_OnBedOverview_UpdatesFilterAndCountAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        // sanity: 初始是 All
        await Assert.That(initial.CurrentBedFilter).IsEqualTo(BedFilter.All);
        await Assert.That(initial.FilteredBedCount).IsEqualTo(BedCatalog.TotalCount);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.SetBedFilter(BedFilter.Open));

        await Assert.That(result.State.CurrentBedFilter).IsEqualTo(BedFilter.Open);
        await Assert.That(result.State.FilteredBedCount).IsEqualTo(BedCatalog.Count(BedFilter.Open));
        await Assert.That(result.State.PageKey).IsEqualTo(PageKey.BedOverview);
        await Assert.That(result.State.ActionLog).Contains("床位筛选已切换为");
    }

    /// <summary>
    /// SetBedFilter 派发到非 BedOverview 卡片时 reducer 静默忽略：state 与传入实例相等，FilteredBedCount 不变。
    /// </summary>
    [Test]
    public async Task SetBedFilter_IgnoredOnNonBedOverviewCardAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.NursingTaskBoard);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.SetBedFilter(BedFilter.Occupied));

        // reducer 短路：state 应等于传入的实例
        await Assert.That(result.State).IsEqualTo(initial);
    }

    /// <summary>
    /// SetBedFilter 派发到 BedOverview 卡片但 Filter 与当前相同：reducer 返回相同 state（无变化）。
    /// </summary>
    [Test]
    public async Task SetBedFilter_SameValue_IsNoopAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);
        // initial.CurrentBedFilter = All

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.SetBedFilter(BedFilter.All));

        await Assert.That(result.State).IsEqualTo(initial);
    }

    /// <summary>
    /// 切换为 Occupied 后 FilteredBedCount 反映 Occupied 床位数；与 BedCatalog.Count 一致。
    /// </summary>
    [Test]
    public async Task SetBedFilter_Occupied_CountMatchesCatalogAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.SetBedFilter(BedFilter.Occupied));

        await Assert.That(result.State.CurrentBedFilter).IsEqualTo(BedFilter.Occupied);
        await Assert.That(result.State.FilteredBedCount).IsEqualTo(BedCatalog.Count(BedFilter.Occupied));
        // 数值应为正（演示数据刻意生成了占用床位）
        await Assert.That(result.State.FilteredBedCount).IsGreaterThan(0);
    }

    private static CardState NewState(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)!;
        return CardState.FromDefinition(definition);
    }
}
