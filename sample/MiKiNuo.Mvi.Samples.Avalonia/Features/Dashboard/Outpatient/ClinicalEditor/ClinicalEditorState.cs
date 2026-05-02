using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑状态。
/// </summary>
/// <param name="PatientName">患者姓名。</param>
/// <param name="Diagnosis">诊断内容。</param>
/// <param name="RiskLevel">风险等级。</param>
/// <param name="SaveMessage">保存提示。</param>
/// <param name="CanSave">是否可以保存。</param>
public sealed record ClinicalEditorState(
    string PatientName,
    string Diagnosis,
    string RiskLevel,
    string SaveMessage,
    bool CanSave) : IMviState
{
    /// <summary>
    /// 获取初始病历编辑状态。
    /// </summary>
    public static ClinicalEditorState Initial { get; } = new("未选择患者", string.Empty, "未评估", "等待接诊患者。", false);
}
