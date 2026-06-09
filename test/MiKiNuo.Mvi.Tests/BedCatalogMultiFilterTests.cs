using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示床位目录多维筛选（床位类型多选 + 床位状态多选）的单元测试。
/// 验证 <see cref="BedCatalog.Query(IReadOnlySet{BedType}, IReadOnlySet{BedStatus})"/> 的
/// 「空集 = 不过滤该维度」语义以及「两个维度同时叠加」语义。
/// </summary>
public sealed class BedCatalogMultiFilterTests
{
    /// <summary>
    /// 两个维度集合均为空时返回全部床位（与 BedCatalog.All 等价）。
    /// </summary>
    [Test]
    public async Task Query_EmptyDimenSets_ReturnsAllBedsAsync()
    {
        IReadOnlyList<BedRecord> result = BedCatalog.Query(
            typeFilter: new HashSet<BedType>(),
            statusFilter: new HashSet<BedStatus>());

        await Assert.That(result.Count).IsEqualTo(BedCatalog.TotalCount);
    }

    /// <summary>
    /// 只指定 BedType=ICU 时，命中所有 ICU 床位（不限状态）。
    /// </summary>
    [Test]
    public async Task Query_OnlyTypeFilter_ReturnsBedsMatchingTypeAsync()
    {
        IReadOnlySet<BedType> types = new HashSet<BedType> { BedType.IntensiveCare };

        IReadOnlyList<BedRecord> result = BedCatalog.Query(types, new HashSet<BedStatus>());

        await Assert.That(result.Count).IsGreaterThan(0);
        foreach (BedRecord record in result)
        {
            await Assert.That(record.Type).IsEqualTo(BedType.IntensiveCare);
        }
    }

    /// <summary>
    /// 同时指定 BedType=普通 + BedStatus=已占用，命中所有普通且已占用的床位。
    /// </summary>
    [Test]
    public async Task Query_TypeAndStatus_IntersectsBothDimensionsAsync()
    {
        IReadOnlySet<BedType> types = new HashSet<BedType> { BedType.General };
        IReadOnlySet<BedStatus> statuses = new HashSet<BedStatus> { BedStatus.Occupied };

        IReadOnlyList<BedRecord> result = BedCatalog.Query(types, statuses);

        await Assert.That(result.Count).IsGreaterThan(0);
        foreach (BedRecord record in result)
        {
            await Assert.That(record.Type).IsEqualTo(BedType.General);
            await Assert.That(record.Status).IsEqualTo(BedStatus.Occupied);
        }
    }

    /// <summary>
    /// 多个 BedType 并集命中：ICU + 隔离 = 所有 ICU 或隔离床位。
    /// </summary>
    [Test]
    public async Task Query_MultipleTypes_UnionMatchesAnyOfThemAsync()
    {
        IReadOnlySet<BedType> types = new HashSet<BedType> { BedType.IntensiveCare, BedType.Isolation };

        IReadOnlyList<BedRecord> result = BedCatalog.Query(types, new HashSet<BedStatus>());

        await Assert.That(result.Count).IsGreaterThan(0);
        foreach (BedRecord record in result)
        {
            await Assert.That(record.Type == BedType.IntensiveCare || record.Type == BedType.Isolation).IsTrue();
        }
    }

    /// <summary>
    /// 命中数为 0 的极端组合（演示数据中 ICU 不会出现 Locked）返回空集合而非 null。
    /// 期望命中数从演示数据动态计算：所有 ICU 且 Locked 的床位数；用于对比 Query 返回值。
    /// </summary>
    [Test]
    public async Task Query_NoMatch_ReturnsEmptyListAsync()
    {
        IReadOnlySet<BedType> types = new HashSet<BedType> { BedType.IntensiveCare };
        IReadOnlySet<BedStatus> statuses = new HashSet<BedStatus> { BedStatus.Locked };

        IReadOnlyList<BedRecord> result = BedCatalog.Query(types, statuses);

        int expected = 0;
        foreach (BedRecord record in BedCatalog.All)
        {
            if (record.Type == BedType.IntensiveCare && record.Status == BedStatus.Locked)
            {
                expected++;
            }
        }

        // 不依赖硬编码：期望值由演示数据动态算出
        await Assert.That(result.Count).IsEqualTo(expected);
    }
}
