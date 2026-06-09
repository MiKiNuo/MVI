namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示一条床位记录（不可变）。
/// 由 <see cref="BedCatalog"/> 在静态构造时一次性生成，供 BedOverview 卡片筛选与展示使用。
/// </summary>
/// <param name="BedNo">床号（病区缩写 + 房号 + 床号，例如 "东-A-12-08"）。</param>
/// <param name="Ward">病区显示名（例如 "东病区"、"ICU"）。</param>
/// <param name="Type">床位物理 / 临床类型。</param>
/// <param name="Status">床位当前使用状态。</param>
/// <param name="PatientName">当前占用床位的患者姓名；床位非占用时为 null。</param>
/// <param name="PrimaryDoctor">主诊医生姓名；床位非占用时为 null。</param>
public sealed record BedRecord(
    string BedNo,
    string Ward,
    BedType Type,
    BedStatus Status,
    string? PatientName,
    string? PrimaryDoctor);
