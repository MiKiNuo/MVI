using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <see cref="BedRecordRow"/> 包装类型的单元测试。
/// 目的：DataGrid 直接绑定 <see cref="BedRecord"/> 时，<see cref="BedRecord.Type"/> 与
/// <see cref="BedRecord.Status"/> 是枚举，<c>DataGridTextColumn</c> 会调用
/// <c>ToString()</c> 得到英文枚举名（如 "General" / "Occupied"），对中文用户不友好；
/// <see cref="BedRecord.PatientName"/> / <see cref="BedRecord.PrimaryDoctor"/> 在床位空闲时为
/// <c>null</c>，直接绑定会显示空单元格。本包装把这两类展示性问题收敛到一行内：
/// <list type="bullet">
///   <item>类型 / 状态 → 映射为中文短词（普通 / ICU / 隔离 / 康复；开放 / 已占用 / 锁定 / 隔离中）；</item>
///   <item>患者 / 主治为空时 → 显示 "—"。</item>
/// </list>
/// 与 <c>BedOverviewComboBoxRenderingTests</c> 互补：本测试断言数据模型转换；
/// 渲染测试断言 XAML/DataGrid 绑定路径。
/// </summary>
public sealed class BedRecordRowTests
{
    /// <summary>
    /// 已占用床位的 <see cref="BedRecordRow"/>：Type / Status 应当为中文展示名，PatientName / PrimaryDoctor 原样透传。
    /// </summary>
    [Test]
    public async Task BedRecordRow_OccupiedBed_MapsEnumsToChineseDisplayAsync()
    {
        BedRecord record = new(
            BedNo: "东-01-A",
            Ward: "东病区",
            Type: BedType.General,
            Status: BedStatus.Occupied,
            PatientName: "张三",
            PrimaryDoctor: "陈主任医师");

        BedRecordRow row = new(record);

        await Assert.That(row.BedNo).IsEqualTo("东-01-A");
        await Assert.That(row.Ward).IsEqualTo("东病区");
        await Assert.That(row.TypeDisplay).IsEqualTo("普通");
        await Assert.That(row.StatusDisplay).IsEqualTo("已占用");
        await Assert.That(row.PatientName).IsEqualTo("张三");
        await Assert.That(row.PrimaryDoctor).IsEqualTo("陈主任医师");
        await Assert.That(row.Source).IsEqualTo(record);
    }

    /// <summary>
    /// 开放床位（<see cref="BedStatus.Open"/>，无患者）：Type / Status 映射为中文，PatientName / PrimaryDoctor
    /// 在源为 null 时显示占位符 "—"，避免 DataGrid 出现空字符串单元格。
    /// </summary>
    [Test]
    public async Task BedRecordRow_OpenBed_NullPatientFields_ShowEmDashAsync()
    {
        BedRecord record = new(
            BedNo: "I-05-B",
            Ward: "ICU",
            Type: BedType.IntensiveCare,
            Status: BedStatus.Open,
            PatientName: null,
            PrimaryDoctor: null);

        BedRecordRow row = new(record);

        await Assert.That(row.TypeDisplay).IsEqualTo("ICU");
        await Assert.That(row.StatusDisplay).IsEqualTo("开放");
        await Assert.That(row.PatientName).IsEqualTo("—");
        await Assert.That(row.PrimaryDoctor).IsEqualTo("—");
    }

    /// <summary>
    /// 隔离与康复床位同样能映射：保证 4 种 <see cref="BedType"/> 与 4 种 <see cref="BedStatus"/> 全部有非空中文值。
    /// </summary>
    [Test]
    public async Task BedRecordRow_AllBedTypesAndStatuses_HaveChineseDisplayAsync()
    {
        BedType[] allTypes = [BedType.General, BedType.IntensiveCare, BedType.Isolation, BedType.Recovery];
        BedStatus[] allStatuses = [BedStatus.Open, BedStatus.Occupied, BedStatus.Locked, BedStatus.Isolated];

        foreach (BedType type in allTypes)
        {
            BedRecord record = new("A-01", "东", type, BedStatus.Open, null, null);
            BedRecordRow row = new(record);
            await Assert.That(row.TypeDisplay).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(row.TypeDisplay).IsNotEqualTo(type.ToString());
        }

        foreach (BedStatus status in allStatuses)
        {
            BedRecord record = new("A-01", "东", BedType.General, status, null, null);
            BedRecordRow row = new(record);
            await Assert.That(row.StatusDisplay).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(row.StatusDisplay).IsNotEqualTo(status.ToString());
        }
    }

    /// <summary>
    /// 验证 <see cref="BedRecordRow.Source"/> 透传 <see cref="BedRecord"/>：业务流需要从
    /// 行回查原始记录（入院时把 <see cref="BedRecord.BedNo"/> 写回 <c>Patient.BedNo</c>）。
    /// </summary>
    [Test]
    public async Task BedRecordRow_Source_ReturnsUnderlyingRecordAsync()
    {
        BedRecord record = new("西-02-B", "西病区", BedType.Isolation, BedStatus.Isolated, "李四", "林副主任医师");
        BedRecordRow row = new(record);

        await Assert.That(row.Source).IsSameReferenceAs(record);
    }

    /// <summary>
    /// 验证 <see cref="BedRecordRow.ToTypeText(BedType)"/> 静态方法对每种枚举返回稳定中文名（用于 XAML 之外的纯文本场景）。
    /// </summary>
    [Test]
    public async Task BedRecordRow_ToTypeText_AllEnumValuesMappedAsync()
    {
        await Assert.That(BedRecordRow.ToTypeText(BedType.General)).IsEqualTo("普通");
        await Assert.That(BedRecordRow.ToTypeText(BedType.IntensiveCare)).IsEqualTo("ICU");
        await Assert.That(BedRecordRow.ToTypeText(BedType.Isolation)).IsEqualTo("隔离");
        await Assert.That(BedRecordRow.ToTypeText(BedType.Recovery)).IsEqualTo("康复");
    }

    /// <summary>
    /// 验证 <see cref="BedRecordRow.ToStatusText(BedStatus)"/> 静态方法对每种枚举返回稳定中文名。
    /// </summary>
    [Test]
    public async Task BedRecordRow_ToStatusText_AllEnumValuesMappedAsync()
    {
        await Assert.That(BedRecordRow.ToStatusText(BedStatus.Open)).IsEqualTo("开放");
        await Assert.That(BedRecordRow.ToStatusText(BedStatus.Occupied)).IsEqualTo("已占用");
        await Assert.That(BedRecordRow.ToStatusText(BedStatus.Locked)).IsEqualTo("锁定");
        await Assert.That(BedRecordRow.ToStatusText(BedStatus.Isolated)).IsEqualTo("隔离中");
    }
}
