using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI 意图。
/// </summary>
public abstract partial record LabOrderComposerIntent : IMviIntent
{
    /// <summary>
    /// 表示修改患者标识意图。
    /// </summary>
    /// <param name="PatientIdentity">患者标识。</param>
    public sealed partial record ChangePatientIdentity(string PatientIdentity) : LabOrderComposerIntent;

    /// <summary>
    /// 表示修改检验项目意图。
    /// </summary>
    /// <param name="TestItem">检验项目。</param>
    public sealed partial record ChangeTestItem(string TestItem) : LabOrderComposerIntent;

    /// <summary>
    /// 表示修改优先级意图。
    /// </summary>
    /// <param name="PriorityLevel">优先级。</param>
    public sealed partial record ChangePriorityLevel(string PriorityLevel) : LabOrderComposerIntent;

    /// <summary>
    /// 表示修改标本类型意图。
    /// </summary>
    /// <param name="SpecimenType">标本类型。</param>
    public sealed partial record ChangeSpecimenType(string SpecimenType) : LabOrderComposerIntent;

    /// <summary>
    /// 表示修改临床指征意图。
    /// </summary>
    /// <param name="ClinicalIndication">临床指征。</param>
    public sealed partial record ChangeClinicalIndication(string ClinicalIndication) : LabOrderComposerIntent;

    /// <summary>
    /// 表示提交检验医嘱意图。
    /// </summary>
    public sealed partial record SubmitLabOrderForm : LabOrderComposerIntent;

    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : LabOrderComposerIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : LabOrderComposerIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : LabOrderComposerIntent;
}
