using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 CheckBox 多选意图（ToggleBedType / ToggleBedStatus）的变更规约器回归测试。
/// 验证处理器：
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
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(PageKey.BedOverview);

        await store.DispatchAsync(new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(store.CurrentState.SelectedBedTypes.Count).IsEqualTo(1);
        await Assert.That(store.CurrentState.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsTrue();

        int expected = BedCatalog.Query(store.CurrentState.SelectedBedTypes, store.CurrentState.SelectedBedStatuses).Count;
        await Assert.That(store.CurrentState.FilteredBedCount).IsEqualTo(expected);
    }

    /// <summary>
    /// ToggleBedType(BedType.IntensiveCare, false) 在已包含 ICU 的状态下移除 ICU。
    /// </summary>
    [Test]
    public async Task ToggleBedType_False_RemovesTypeAsync()
    {
        CardState initial = NewState(PageKey.BedOverview) with
        {
            SelectedBedTypes = new HashSet<BedType> { BedType.IntensiveCare, BedType.General },
        };
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(initial);

        await store.DispatchAsync(new CardIntent.ToggleBedType(BedType.IntensiveCare, false));

        await Assert.That(store.CurrentState.SelectedBedTypes.Count).IsEqualTo(1);
        await Assert.That(store.CurrentState.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsFalse();
        await Assert.That(store.CurrentState.SelectedBedTypes.Contains(BedType.General)).IsTrue();
    }

    /// <summary>
    /// ToggleBedStatus(BedStatus.Occupied, true) 把已占用加入 SelectedBedStatuses。
    /// </summary>
    [Test]
    public async Task ToggleBedStatus_True_AddsStatusAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(PageKey.BedOverview);

        await store.DispatchAsync(new CardIntent.ToggleBedStatus(BedStatus.Occupied, true));

        await Assert.That(store.CurrentState.SelectedBedStatuses.Count).IsEqualTo(1);
        await Assert.That(store.CurrentState.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();
    }

    /// <summary>
    /// ToggleBedType 派发到非 BedOverview 卡片时处理器静默忽略：state 不变。
    /// </summary>
    [Test]
    public async Task ToggleBedType_IgnoredOnNonBedOverviewAsync()
    {
        CardState initial = NewState(PageKey.NursingTaskBoard);
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(initial);

        await store.DispatchAsync(new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(store.CurrentState).IsEqualTo(initial);
    }

    /// <summary>
    /// ToggleBedStatus 派发到非 BedOverview 卡片时处理器静默忽略。
    /// </summary>
    [Test]
    public async Task ToggleBedStatus_IgnoredOnNonBedOverviewAsync()
    {
        CardState initial = NewState(PageKey.NursingTaskBoard);
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(initial);

        await store.DispatchAsync(new CardIntent.ToggleBedStatus(BedStatus.Locked, true));

        await Assert.That(store.CurrentState).IsEqualTo(initial);
    }

    /// <summary>
    /// 同时 ToggleBedType + ToggleBedStatus 叠加：SelectedBedTypes 与 SelectedBedStatuses 同时生效，
    /// FilteredBedCount 等于 BedCatalog.Query(两类, 两状态) 命中数。
    /// </summary>
    [Test]
    public async Task ToggleBoth_IntersectsBothDimensionsAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(PageKey.BedOverview);

        await store.DispatchAsync(new CardIntent.ToggleBedType(BedType.General, true));
        await store.DispatchAsync(new CardIntent.ToggleBedStatus(BedStatus.Occupied, true));

        await Assert.That(store.CurrentState.SelectedBedTypes.Contains(BedType.General)).IsTrue();
        await Assert.That(store.CurrentState.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();

        int expected = BedCatalog.Query(store.CurrentState.SelectedBedTypes, store.CurrentState.SelectedBedStatuses).Count;
        await Assert.That(store.CurrentState.FilteredBedCount).IsEqualTo(expected);
        await Assert.That(store.CurrentState.FilteredBedCount).IsGreaterThan(0);
    }

    /// <summary>
    /// ActionLog 反映「类型 / 状态已加入或移除」的文案。
    /// </summary>
    [Test]
    public async Task ToggleBedType_UpdatesActionLogAsync()
    {
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = CreateStore(PageKey.BedOverview);

        await store.DispatchAsync(new CardIntent.ToggleBedType(BedType.IntensiveCare, true));

        await Assert.That(store.CurrentState.ActionLog).Contains("ICU");
        await Assert.That(store.CurrentState.ActionLog).Contains("类型");
    }

    private static CardState NewState(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)!;
        return CardState.FromDefinition(definition);
    }

    private static MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> CreateStore(PageKey key)
    {
        return CreateStore(NewState(key));
    }

    private static MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> CreateStore(CardState initial)
    {
        return new MviMutationStore<CardState, CardIntent, CardMutation, CardEffect>(
            initial,
            new CardIntentHandler(DashboardCardRegistry.All),
            new CardMutationReducer(),
            new NoopCardEffectDispatcher());
    }

    private sealed class NoopCardEffectDispatcher : IMviEffectDispatcher<CardEffect>
    {
        /// <summary>
        /// 分发副作用。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步分发过程的任务。</returns>
        public ValueTask DispatchAsync(CardEffect effect, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
