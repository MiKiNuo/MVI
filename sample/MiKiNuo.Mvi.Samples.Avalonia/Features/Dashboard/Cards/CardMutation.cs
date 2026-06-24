using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片 MVI 变更。
/// </summary>
public abstract record CardMutation : IMviMutation<CardState>
{
    /// <summary>设置状态文本。</summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : CardMutation;

    /// <summary>设置详情文本。</summary>
    /// <param name="Value">详情文本。</param>
    [MviMutation(Path = "DetailText")]
    public sealed record SetDetailText(string Value) : CardMutation;

    /// <summary>设置动作日志。</summary>
    /// <param name="Value">动作日志。</param>
    [MviMutation(Path = "ActionLog")]
    public sealed record SetActionLog(string Value) : CardMutation;

    /// <summary>设置主动作可用性。</summary>
    /// <param name="Value">是否可用。</param>
    [MviMutation(Path = "CanPrimaryAction")]
    public sealed record SetCanPrimaryAction(bool Value) : CardMutation;

    /// <summary>设置辅助动作可用性。</summary>
    /// <param name="Value">是否可用。</param>
    [MviMutation(Path = "CanSecondaryAction")]
    public sealed record SetCanSecondaryAction(bool Value) : CardMutation;

    /// <summary>设置表单错误消息。</summary>
    /// <param name="Value">错误消息。</param>
    [MviMutation(Path = "FormErrorMessage")]
    public sealed record SetFormErrorMessage(string Value) : CardMutation;

    /// <summary>设置表单字段值集合。</summary>
    /// <param name="Value">字段值集合。</param>
    [MviMutation(Path = "FormValues")]
    public sealed record SetFormValues(IReadOnlyList<CardFormValueEntry> Value) : CardMutation;

    /// <summary>设置最近入院患者。</summary>
    /// <param name="Value">患者记录。</param>
    [MviMutation(Path = "RecentAdmittedPatient")]
    public sealed record SetRecentAdmittedPatient(Patient? Value) : CardMutation;

    /// <summary>设置床位筛选维度。</summary>
    /// <param name="Value">筛选维度。</param>
    [MviMutation(Path = "CurrentBedFilter")]
    public sealed record SetCurrentBedFilter(BedFilter Value) : CardMutation;

    /// <summary>设置筛选床位数量。</summary>
    /// <param name="Value">床位数量。</param>
    [MviMutation(Path = "FilteredBedCount")]
    public sealed record SetFilteredBedCount(int Value) : CardMutation;

    /// <summary>设置选中的床位类型集合。</summary>
    /// <param name="Value">床位类型集合。</param>
    [MviMutation(Path = "SelectedBedTypes")]
    public sealed record SetSelectedBedTypes(IReadOnlySet<BedType> Value) : CardMutation;

    /// <summary>设置选中的床位状态集合。</summary>
    /// <param name="Value">床位状态集合。</param>
    [MviMutation(Path = "SelectedBedStatuses")]
    public sealed record SetSelectedBedStatuses(IReadOnlySet<BedStatus> Value) : CardMutation;
}
