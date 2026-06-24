using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑 MVI 变更。
/// </summary>
public abstract record ClinicalEditorMutation : IMviMutation<ClinicalEditorState>
{
    /// <summary>
    /// 表示设置患者姓名的变更。
    /// </summary>
    /// <param name="Value">患者姓名。</param>
    [MviMutation(Path = "PatientName")]
    public sealed record SetPatientName(string Value) : ClinicalEditorMutation;

    /// <summary>
    /// 表示设置诊断内容的变更。
    /// </summary>
    /// <param name="Value">诊断内容。</param>
    [MviMutation(Path = "Diagnosis")]
    public sealed record SetDiagnosis(string Value) : ClinicalEditorMutation;

    /// <summary>
    /// 表示设置风险等级的变更。
    /// </summary>
    /// <param name="Value">风险等级。</param>
    [MviMutation(Path = "RiskLevel")]
    public sealed record SetRiskLevel(string Value) : ClinicalEditorMutation;

    /// <summary>
    /// 表示设置保存提示的变更。
    /// </summary>
    /// <param name="Value">保存提示。</param>
    [MviMutation(Path = "SaveMessage")]
    public sealed record SetSaveMessage(string Value) : ClinicalEditorMutation;

    /// <summary>
    /// 表示设置可保存状态的变更。
    /// </summary>
    /// <param name="Value">是否可保存。</param>
    [MviMutation(Path = "CanSave")]
    public sealed record SetCanSave(bool Value) : ClinicalEditorMutation;
}
