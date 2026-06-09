using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="CardState"/> 多选筛选字段的回归测试。
/// 验证初始状态有默认空集合（表示「不过滤」），以及替代语义：与 ComboBox 单选 <see cref="CardState.CurrentBedFilter"/> 共存。
/// </summary>
public sealed class CardStateMultiFilterTests
{
    /// <summary>
    /// BedOverview 卡片初始状态：SelectedBedTypes 与 SelectedBedStatuses 都为空集合（语义：不过滤该维度）。
    /// </summary>
    [Test]
    public async Task BedOverview_InitialState_EmptySelectedTypesAndStatusesAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState state = CardState.FromDefinition(definition);

        await Assert.That(state.SelectedBedTypes.Count).IsEqualTo(0);
        await Assert.That(state.SelectedBedStatuses.Count).IsEqualTo(0);
    }

    /// <summary>
    /// 非 BedOverview 卡片的多选字段同样初始化为空集合（保持各卡片独立）。
    /// </summary>
    [Test]
    public async Task NonBedOverview_InitialState_EmptySelectedTypesAndStatusesAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.NursingTaskBoard)!;
        CardState state = CardState.FromDefinition(definition);

        await Assert.That(state.SelectedBedTypes.Count).IsEqualTo(0);
        await Assert.That(state.SelectedBedStatuses.Count).IsEqualTo(0);
    }

    /// <summary>
    /// with 表达式能正确复制出包含新 SelectedBedTypes/Statuses 的新 state，
    /// 旧 state 保持空集合（值类型 record 的不可变语义）。
    /// </summary>
    [Test]
    public async Task With_ReplacesSelectedCollections_AndOriginalStaysEmptyAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState original = CardState.FromDefinition(definition);

        IReadOnlySet<BedType> nextTypes = new HashSet<BedType> { BedType.IntensiveCare, BedType.Isolation };
        IReadOnlySet<BedStatus> nextStatuses = new HashSet<BedStatus> { BedStatus.Occupied };

        CardState next = original with
        {
            SelectedBedTypes = nextTypes,
            SelectedBedStatuses = nextStatuses,
        };

        // 旧 state 不变
        await Assert.That(original.SelectedBedTypes.Count).IsEqualTo(0);
        await Assert.That(original.SelectedBedStatuses.Count).IsEqualTo(0);

        // 新 state 携带新集合
        await Assert.That(next.SelectedBedTypes.Count).IsEqualTo(2);
        await Assert.That(next.SelectedBedStatuses.Count).IsEqualTo(1);
        await Assert.That(next.SelectedBedTypes.Contains(BedType.IntensiveCare)).IsTrue();
        await Assert.That(next.SelectedBedStatuses.Contains(BedStatus.Occupied)).IsTrue();
    }

    /// <summary>
    /// CurrentBedFilter（ComboBox 单选）与 SelectedBedTypes/Statuses（CheckBox 多选）字段共存于同一 record。
    /// </summary>
    [Test]
    public async Task State_HasBothSingleAndMultiFilterFieldsAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState state = CardState.FromDefinition(definition);

        // 单选
        await Assert.That(state.CurrentBedFilter).IsEqualTo(BedFilter.All);

        // 多选
        await Assert.That(state.SelectedBedTypes).IsNotNull();
        await Assert.That(state.SelectedBedStatuses).IsNotNull();
    }
}
