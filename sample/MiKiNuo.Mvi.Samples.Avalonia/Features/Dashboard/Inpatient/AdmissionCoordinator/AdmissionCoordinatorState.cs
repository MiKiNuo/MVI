using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI 状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="MainValue">核心指标。</param>
/// <param name="StatusText">状态文本。</param>
/// <param name="DetailText">详情文本。</param>
/// <param name="ActionLog">动作日志。</param>
/// <param name="PrimaryActionText">主动作文本。</param>
/// <param name="SecondaryActionText">辅助动作文本。</param>
/// <param name="CanPrimaryAction">是否允许执行主动作。</param>
/// <param name="CanSecondaryAction">是否允许执行辅助动作。</param>
/// <param name="PatientName">患者姓名。</param>
/// <param name="PatientAge">患者年龄。</param>
/// <param name="AdmissionDiagnosis">入院诊断。</param>
/// <param name="TargetBedNo">目标床号。</param>
/// <param name="NurseNote">护士交接备注。</param>
/// <param name="ConfirmAdmissionText">确认入院按钮文本。</param>
/// <param name="CanConfirmAdmission">是否允许确认入院。</param>
public sealed record AdmissionCoordinatorState(
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction,
    string PatientName,
    string PatientAge,
    string AdmissionDiagnosis,
    string TargetBedNo,
    string NurseNote,
    string ConfirmAdmissionText,
    bool CanConfirmAdmission) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static AdmissionCoordinatorState Initial { get; } = new(
        "入院登记 MVI",
        "待入院 37 人",
        "等待护士录入入院资料",
        "护士录入患者、诊断、床号和交接备注，确认后由副作用提交给 Mediator，再分发给床位总览、护理任务和病区风险 MVI。",
        "等待录入入院资料。",
        "确认入院",
        "退回急诊",
        true,
        true,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "提交入院登记",
        false);
}
