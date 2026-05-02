using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 意图。
/// </summary>
public abstract partial record PatientSearchIntent : IMviIntent
{
    /// <summary>
    /// 表示修改检索关键字意图。
    /// </summary>
    /// <param name="QueryText">检索关键字。</param>
    public sealed partial record ChangeQueryText(string QueryText) : PatientSearchIntent;

    /// <summary>
    /// 表示执行患者检索意图。
    /// </summary>
    public sealed partial record SearchPatient : PatientSearchIntent;

    /// <summary>
    /// 表示选择当前第一位患者意图。
    /// </summary>
    public sealed partial record SelectFirstPatient : PatientSearchIntent;

    /// <summary>
    /// 表示接收父页面或兄弟 MVI 外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : PatientSearchIntent;
}
