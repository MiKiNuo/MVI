namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示床位的运行时使用状态。
/// 描述「床位此刻能否被新患者占用」的事实状态，与 <see cref="BedFilter"/> 一一对应（除 <see cref="BedFilter.All"/> 外）。
/// </summary>
public enum BedStatus
{
    /// <summary>开放（可入住）。</summary>
    Open = 1,

    /// <summary>已占用（当前有患者）。</summary>
    Occupied = 2,

    /// <summary>锁定（保留 / 维护 / 待清理）。</summary>
    Locked = 3,

    /// <summary>隔离（传染 / 免疫低下专用）。</summary>
    Isolated = 4,
}
