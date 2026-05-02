using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒意图。
/// </summary>
public abstract partial record ClinicalReminderIntent : IMviIntent
{
    /// <summary>
    /// 表示加载患者提醒意图。
    /// </summary>
    /// <param name="PatientName">患者姓名。</param>
    public sealed partial record LoadPatient(string PatientName) : ClinicalReminderIntent;

    /// <summary>
    /// 表示处理首要提醒意图。
    /// </summary>
    public sealed partial record ResolvePrimaryAlert : ClinicalReminderIntent;
}
