using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒状态。
/// </summary>
/// <param name="PatientName">患者姓名。</param>
/// <param name="Alerts">提醒集合。</param>
/// <param name="PrimaryAlert">首要提醒。</param>
/// <param name="HasAlert">是否存在提醒。</param>
public sealed record ClinicalReminderState(
    string PatientName,
    IReadOnlyList<string> Alerts,
    string PrimaryAlert,
    bool HasAlert) : IMviState
{
    /// <summary>
    /// 获取初始临床提醒状态。
    /// </summary>
    public static ClinicalReminderState Initial { get; } = new("未选择患者", ["等待患者接诊后加载提醒。"], "无", false);
}
