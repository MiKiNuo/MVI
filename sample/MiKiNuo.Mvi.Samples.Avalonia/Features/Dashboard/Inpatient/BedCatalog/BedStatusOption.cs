using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示 <see cref="BedStatus"/> 在 CheckBox 中显示的展示项。
/// 与 <see cref="BedTypeOption"/> 同构：枚举值 + 中文展示名。
/// </summary>
/// <param name="Value">床位状态枚举值。</param>
/// <param name="DisplayName">UI 展示用中文名（与 <see cref="BedRecordRow.ToStatusText"/> 保持一致）。</param>
public sealed record BedStatusOption(BedStatus Value, string DisplayName)
{
    /// <summary>获取全部 <see cref="BedStatus"/> 的展示项（顺序与枚举定义一致）。</summary>
    public static IReadOnlyList<BedStatusOption> All { get; } = new BedStatusOption[]
    {
        new(BedStatus.Open, BedRecordRow.ToStatusText(BedStatus.Open)),
        new(BedStatus.Occupied, BedRecordRow.ToStatusText(BedStatus.Occupied)),
        new(BedStatus.Locked, BedRecordRow.ToStatusText(BedStatus.Locked)),
        new(BedStatus.Isolated, BedRecordRow.ToStatusText(BedStatus.Isolated)),
    };
}
