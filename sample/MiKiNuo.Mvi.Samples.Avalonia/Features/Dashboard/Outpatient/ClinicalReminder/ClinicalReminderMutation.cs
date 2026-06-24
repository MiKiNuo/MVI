using System.Collections.Generic;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒 MVI 变更。
/// </summary>
public abstract record ClinicalReminderMutation : IMviMutation<ClinicalReminderState>
{
    /// <summary>
    /// 表示设置患者姓名的变更。
    /// </summary>
    /// <param name="Value">患者姓名。</param>
    [MviMutation(Path = "PatientName")]
    public sealed record SetPatientName(string Value) : ClinicalReminderMutation;

    /// <summary>
    /// 表示设置提醒列表的变更。
    /// </summary>
    /// <param name="Value">提醒列表。</param>
    [MviMutation(Path = "Alerts")]
    public sealed record SetAlerts(IReadOnlyList<string> Value) : ClinicalReminderMutation;

    /// <summary>
    /// 表示设置首要提醒的变更。
    /// </summary>
    /// <param name="Value">首要提醒文本。</param>
    [MviMutation(Path = "PrimaryAlert")]
    public sealed record SetPrimaryAlert(string Value) : ClinicalReminderMutation;

    /// <summary>
    /// 表示设置是否有提醒的变更。
    /// </summary>
    /// <param name="Value">是否有提醒。</param>
    [MviMutation(Path = "HasAlert")]
    public sealed record SetHasAlert(bool Value) : ClinicalReminderMutation;
}
