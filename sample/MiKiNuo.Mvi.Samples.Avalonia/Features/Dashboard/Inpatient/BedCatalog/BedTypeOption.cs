using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示 <see cref="BedType"/> 在 CheckBox 中显示的展示项。
/// 把枚举值与中文显示名绑定，便于 Avalonia <c>CheckBox.Content</c> 直接绑定 <c>DisplayName</c>，
/// 并通过 <c>IsChecked</c> 双向绑定 + <c>ToggleBedTypeCommand</c> 派发多选意图。
/// </summary>
/// <param name="Value">床位类型枚举值。</param>
/// <param name="DisplayName">UI 展示用中文名（与 <see cref="BedRecordRow.ToTypeText"/> 保持一致）。</param>
public sealed record BedTypeOption(BedType Value, string DisplayName)
{
    /// <summary>获取全部 <see cref="BedType"/> 的展示项（顺序与枚举定义一致）。</summary>
    public static IReadOnlyList<BedTypeOption> All { get; } = new BedTypeOption[]
    {
        new(BedType.General, BedRecordRow.ToTypeText(BedType.General)),
        new(BedType.IntensiveCare, BedRecordRow.ToTypeText(BedType.IntensiveCare)),
        new(BedType.Isolation, BedRecordRow.ToTypeText(BedType.Isolation)),
        new(BedType.Recovery, BedRecordRow.ToTypeText(BedType.Recovery)),
    };
}
