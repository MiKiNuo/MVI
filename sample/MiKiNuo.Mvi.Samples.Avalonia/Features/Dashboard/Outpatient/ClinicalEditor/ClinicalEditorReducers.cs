using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑规约器。
/// </summary>
public sealed partial class ClinicalEditorReducer
    : MviReducerBase<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorEffect>
{
    /// <summary>
    /// 处理加载患者意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">加载患者意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> Reduce(
        ClinicalEditorState state,
        ClinicalEditorIntent.LoadPatient intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state with
        {
            PatientName = intent.PatientName,
            Diagnosis = "",
            RiskLevel = intent.PatientName.Contains("胸闷", StringComparison.Ordinal) ? "高危" : "普通",
            SaveMessage = "已载入患者上下文，请录入诊断。",
            CanSave = false
        });
    }

    /// <summary>
    /// 处理修改诊断意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">修改诊断意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> Reduce(
        ClinicalEditorState state,
        ClinicalEditorIntent.ChangeDiagnosis intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state with
        {
            Diagnosis = intent.Diagnosis,
            SaveMessage = "诊断已变更，等待保存。",
            CanSave = !string.IsNullOrWhiteSpace(intent.Diagnosis) && state.PatientName != "未选择患者"
        });
    }

    /// <summary>
    /// 处理保存草稿意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">保存草稿意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> Reduce(
        ClinicalEditorState state,
        ClinicalEditorIntent.SaveDraft intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state with
        {
            SaveMessage = $"{DateTime.Now:HH:mm:ss} 已保存 {state.PatientName} 的门诊病历草稿。"
        });
    }
}
