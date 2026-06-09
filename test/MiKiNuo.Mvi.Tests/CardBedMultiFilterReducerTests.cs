using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 CheckBox 多选意图（ToggleBedType / ToggleBedStatus）的归约器回归测试。
/// 验证 reducer：
/// 1. 仅 BedOverview 卡片生效（其他卡片短路忽略）。
/// 2. IsSelected=true 添加、false 移除集合项。
/// 3. 多选后 FilteredBedCount 等于 BedCatalog.Query 新筛选命中数。
/// </summary>
public sealed class CardBedMultiFilterReducerTests
{
    /// <summary>
    /// ToggleBedType(BedType.IntensiveCare, true) 在 BedOverview 卡片把 ICU 加入 SelectedBedTypes；
    /// FilteredBedCount 反映多维 Query 的命中数。
    /// </summary>
    [Test]
    public async Task ToggleBedType_True_AddsTypeAndRecomputesCountAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(result.State.SelectedBedTypes.Count).IsEqualTo(1);
        await Assert.That(result.State.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsTrue();

        int expected = BedCatalog.Query(result.State.SelectedBedTypes, result.State.SelectedBedStatuses).Count;
        await Assert.That(result.State.FilteredBedCount).IsEqualTo(expected);
    }

    /// <summary>
    /// ToggleBedType(BedType.IntensiveCare, false) 在已包含 ICU 的状态下移除 ICU。
    /// </summary>
    [Test]
    public async Task ToggleBedType_False_RemovesTypeAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview) with
        {
            SelectedBedTypes = new HashSet<BedType> { BedType.IntensiveCare, BedType.General },
        };

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedType(BedType.IntensiveCare, false));

        await Assert.That(result.State.SelectedBedTypes.Count).IsEqualTo(1);
        await Assert.That(result.State.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsFalse();
        await Assert.That(result.State.SelectedBedTypes.Contains(BedType.General)).IsTrue();
    }

    /// <summary>
    /// ToggleBedStatus(BedStatus.Occupied, true) 把已占用加入 SelectedBedStatuses。
    /// </summary>
    [Test]
    public async Task ToggleBedStatus_True_AddsStatusAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedStatus(BedStatus.Occupied, true));

        await Assert.That(result.State.SelectedBedStatuses.Count).IsEqualTo(1);
        await Assert.That(result.State.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();
    }

    /// <summary>
    /// ToggleBedType 派发到非 BedOverview 卡片时 reducer 静默忽略：state 不变。
    /// </summary>
    [Test]
    public async Task ToggleBedType_IgnoredOnNonBedOverviewAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.NursingTaskBoard);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(result.State).IsEqualTo(initial);
    }

    /// <summary>
    /// ToggleBedStatus 派发到非 BedOverview 卡片时 reducer 静默忽略。
    /// </summary>
    [Test]
    public async Task ToggleBedStatus_IgnoredOnNonBedOverviewAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.NursingTaskBoard);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedStatus(BedStatus.Locked, true));

        await Assert.That(result.State).IsEqualTo(initial);
    }

    /// <summary>
    /// 同时 ToggleBedType + ToggleBedStatus 叠加：SelectedBedTypes 与 SelectedBedStatuses 同时生效，
    /// FilteredBedCount 等于 BedCatalog.Query(两类, 两状态) 命中数。
    /// </summary>
    [Test]
    public async Task ToggleBoth_IntersectsBothDimensionsAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        MviReduceResult<CardState, CardEffect> afterType = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedType(BedType.General, true));
        MviReduceResult<CardState, CardEffect> afterStatus = reducer.Reduce(
            afterType.State,
            new CardIntent.ToggleBedStatus(BedStatus.Occupied, true));

        await Assert.That(afterStatus.State.SelectedBedTypes.Contains(BedType.General)).IsTrue();
        await Assert.That(afterStatus.State.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();

        int expected = BedCatalog.Query(afterStatus.State.SelectedBedTypes, afterStatus.State.SelectedBedStatuses).Count;
        await Assert.That(afterStatus.State.FilteredBedCount).IsEqualTo(expected);
        await Assert.That(afterStatus.State.FilteredBedCount).IsGreaterThan(0);
    }

    /// <summary>
    /// ActionLog 反映「类型 / 状态已加入或移除」的文案。
    /// </summary>
    [Test]
    public async Task ToggleBedType_UpdatesActionLogAsync()
    {
        CardReducer reducer = new();
        CardState initial = NewState(PageKey.BedOverview);

        MviReduceResult<CardState, CardEffect> result = reducer.Reduce(
            initial,
            new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(result.State.ActionLog).Contains("ICU");
        await Assert.That(result.State.ActionLog).Contains("类型");
    }

    private static CardState NewState(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)!;
        return CardState.FromDefinition(definition);
    }
}
