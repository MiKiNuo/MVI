using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI 意图。
/// </summary>
public abstract partial record PrescriptionReviewBoardIntent : IMviIntent
{
    /// <summary>
    /// 表示修改处方号意图。
    /// </summary>
    /// <param name="PrescriptionNo">处方号。</param>
    public sealed partial record ChangePrescriptionNo(string PrescriptionNo) : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示修改患者姓名意图。
    /// </summary>
    /// <param name="PatientName">患者姓名。</param>
    public sealed partial record ChangePatientName(string PatientName) : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示修改药品名称意图。
    /// </summary>
    /// <param name="DrugName">药品名称。</param>
    public sealed partial record ChangeDrugName(string DrugName) : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示修改剂量用法意图。
    /// </summary>
    /// <param name="DoseText">剂量用法。</param>
    public sealed partial record ChangeDoseText(string DoseText) : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示修改过敏史意图。
    /// </summary>
    /// <param name="AllergyHistory">过敏史。</param>
    public sealed partial record ChangeAllergyHistory(string AllergyHistory) : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示提交处方审核意图。
    /// </summary>
    public sealed partial record SubmitPrescriptionReviewForm : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : PrescriptionReviewBoardIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : PrescriptionReviewBoardIntent;
}
