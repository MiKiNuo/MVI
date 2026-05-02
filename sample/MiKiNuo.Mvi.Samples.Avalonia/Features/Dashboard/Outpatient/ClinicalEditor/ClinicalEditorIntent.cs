using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑意图。
/// </summary>
public abstract partial record ClinicalEditorIntent : IMviIntent
{
    /// <summary>
    /// 表示加载患者意图。
    /// </summary>
    /// <param name="PatientName">患者姓名。</param>
    public sealed partial record LoadPatient(string PatientName) : ClinicalEditorIntent;

    /// <summary>
    /// 表示修改诊断意图。
    /// </summary>
    /// <param name="Diagnosis">诊断内容。</param>
    public sealed partial record ChangeDiagnosis(string Diagnosis) : ClinicalEditorIntent;

    /// <summary>
    /// 表示保存草稿意图。
    /// </summary>
    public sealed partial record SaveDraft : ClinicalEditorIntent;
}
