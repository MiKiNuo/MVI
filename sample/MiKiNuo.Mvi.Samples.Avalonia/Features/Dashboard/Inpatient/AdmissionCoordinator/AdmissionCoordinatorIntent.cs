using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI 意图。
/// </summary>
public abstract partial record AdmissionCoordinatorIntent : IMviIntent
{
    /// <summary>
    /// 表示修改患者姓名意图。
    /// </summary>
    /// <param name="PatientName">患者姓名。</param>
    public sealed partial record ChangePatientName(string PatientName) : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示修改患者年龄意图。
    /// </summary>
    /// <param name="PatientAge">患者年龄。</param>
    public sealed partial record ChangePatientAge(string PatientAge) : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示修改入院诊断意图。
    /// </summary>
    /// <param name="AdmissionDiagnosis">入院诊断。</param>
    public sealed partial record ChangeAdmissionDiagnosis(string AdmissionDiagnosis) : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示修改目标床号意图。
    /// </summary>
    /// <param name="TargetBedNo">目标床号。</param>
    public sealed partial record ChangeTargetBedNo(string TargetBedNo) : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示修改护士交接备注意图。
    /// </summary>
    /// <param name="NurseNote">护士交接备注。</param>
    public sealed partial record ChangeNurseNote(string NurseNote) : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示提交入院登记表单意图。
    /// </summary>
    public sealed partial record SubmitAdmissionForm : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : AdmissionCoordinatorIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : AdmissionCoordinatorIntent;
}
