namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示床位的物理 / 临床类型。
/// 决定可接收的患者类别与配套设备。
/// </summary>
public enum BedType
{
    /// <summary>普通床位。</summary>
    General = 1,

    /// <summary>ICU 床位（重症监护）。</summary>
    IntensiveCare = 2,

    /// <summary>隔离床位（传染 / 免疫低下）。</summary>
    Isolation = 3,

    /// <summary>手术恢复床位（PACU）。</summary>
    Recovery = 4,
}
