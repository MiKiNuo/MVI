namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示 <see cref="BedFilter"/> 在 ComboBox 中显示的展示项。
/// 把枚举值与中文显示名绑定，便于 Avalonia <c>ComboBox.ItemTemplate</c> 直接绑定 <c>DisplayName</c>。
/// </summary>
/// <param name="Value">筛选枚举值（与 <see cref="BedFilter"/> 一一对应）。</param>
/// <param name="DisplayName">UI 展示用中文名。</param>
public sealed record BedFilterOption(BedFilter Value, string DisplayName)
{
    /// <summary>获取全部 <see cref="BedFilter"/> 的展示项（顺序与枚举定义一致）。</summary>
    public static IReadOnlyList<BedFilterOption> All { get; } = new BedFilterOption[]
    {
        new(BedFilter.All, "全部床位"),
        new(BedFilter.Open, "开放（可入住）"),
        new(BedFilter.Occupied, "已占用"),
        new(BedFilter.Locked, "锁定（保留 / 维护）"),
        new(BedFilter.Isolated, "隔离"),
    };
}
