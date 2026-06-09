using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示 BedOverview 卡片 DataGrid 的行包装：把 <see cref="BedRecord"/> 的枚举 / 可空字段
/// 转换为适合 DataGrid 展示的中文短词。
/// <para>
/// 设计动机：<see cref="BedRecord"/> 是不可变 record，承载领域原始数据；
/// <see cref="BedRecord.Type"/> / <see cref="BedRecord.Status"/> 是枚举，<c>DataGridTextColumn</c>
/// 会调用 <c>ToString()</c> 得到英文名（"General" / "Occupied"），对中文用户不友好；
/// <see cref="BedRecord.PatientName"/> / <see cref="BedRecord.PrimaryDoctor"/> 在床位空闲时为
/// <c>null</c>，直接绑定显示空字符串。包装类型把这两类展示性问题收敛到一行内，XAML 直接
/// 绑定 <c>{Binding TypeDisplay}</c> 等即可。
/// </para>
/// <para>
/// 不实现 <see cref="System.ComponentModel.INotifyPropertyChanged"/>：行是只读快照，
/// state -> <see cref="BedCatalog.Query(BedFilter)"/> 重新生成新列表时由
/// <see cref="CardViewModel"/> 整体替换 <c>FilteredBedRows</c> 引用，DataGrid 重新建行。
/// </para>
/// </summary>
public sealed class BedRecordRow
{
    private const string EmptyPlaceholder = "—";

    private readonly BedRecord _record;

    /// <summary>
    /// 初始化行包装。
    /// </summary>
    /// <param name="record">原始床位记录。</param>
    public BedRecordRow(BedRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _record = record;
    }

    /// <summary>获取底层 <see cref="BedRecord"/>（业务流回查原始数据，例如入院时回填床号）。</summary>
    public BedRecord Source => _record;

    /// <summary>床号（原样透传，例如 "东-A-12-08"）。</summary>
    public string BedNo => _record.BedNo;

    /// <summary>病区显示名（原样透传，例如 "东病区" / "ICU"）。</summary>
    public string Ward => _record.Ward;

    /// <summary>床位类型的中文展示名（普通 / ICU / 隔离 / 康复）。</summary>
    public string TypeDisplay => ToTypeText(_record.Type);

    /// <summary>床位状态的中文展示名（开放 / 已占用 / 锁定 / 隔离中）。</summary>
    public string StatusDisplay => ToStatusText(_record.Status);

    /// <summary>当前占用床位的患者姓名；床位空闲时返回占位符 "—"。</summary>
    public string PatientName => _record.PatientName ?? EmptyPlaceholder;

    /// <summary>主诊医生姓名；床位空闲时返回占位符 "—"。</summary>
    public string PrimaryDoctor => _record.PrimaryDoctor ?? EmptyPlaceholder;

    /// <summary>
    /// 把 <see cref="BedType"/> 枚举映射为中文展示名。
    /// </summary>
    /// <param name="type">床位类型枚举。</param>
    /// <returns>中文展示名；未知值走 <c>ToString()</c> 兜底。</returns>
    public static string ToTypeText(BedType type)
    {
        return type switch
        {
            BedType.General => "普通",
            BedType.IntensiveCare => "ICU",
            BedType.Isolation => "隔离",
            BedType.Recovery => "康复",
            _ => type.ToString(),
        };
    }

    /// <summary>
    /// 把 <see cref="BedStatus"/> 枚举映射为中文展示名。
    /// </summary>
    /// <param name="status">床位状态枚举。</param>
    /// <returns>中文展示名；未知值走 <c>ToString()</c> 兜底。</returns>
    public static string ToStatusText(BedStatus status)
    {
        return status switch
        {
            BedStatus.Open => "开放",
            BedStatus.Occupied => "已占用",
            BedStatus.Locked => "锁定",
            BedStatus.Isolated => "隔离中",
            _ => status.ToString(),
        };
    }
}
