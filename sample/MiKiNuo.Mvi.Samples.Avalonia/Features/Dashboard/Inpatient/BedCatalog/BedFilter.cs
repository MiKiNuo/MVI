namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示床位筛选维度。
/// 控制 <see cref="BedCatalog"/> 的可见集合，与 <see cref="BedStatus"/> 一一对应（除 <see cref="All"/> 外）。
/// 状态字段写入 <see cref="MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards.CardState.CurrentBedFilter"/>。
/// </summary>
public enum BedFilter
{
    /// <summary>全部床位（不过滤）。</summary>
    All = 0,

    /// <summary>仅开放（可入住）床位。</summary>
    Open = 1,

    /// <summary>仅已占用床位。</summary>
    Occupied = 2,

    /// <summary>仅锁定（保留 / 维护）床位。</summary>
    Locked = 3,

    /// <summary>仅隔离床位。</summary>
    Isolated = 4,
}
