using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI 状态。
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
/// <param name="PrescriptionNo">处方号。</param>
/// <param name="PatientName">患者姓名。</param>
/// <param name="DrugName">药品名称。</param>
/// <param name="DoseText">剂量用法。</param>
/// <param name="AllergyHistory">过敏史。</param>
/// <param name="ApprovePrescriptionText">审核按钮文本。</param>
/// <param name="CanApprovePrescription">是否允许审核处方。</param>
public sealed record PrescriptionReviewBoardState(
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction,
    string PrescriptionNo,
    string PatientName,
    string DrugName,
    string DoseText,
    string AllergyHistory,
    string ApprovePrescriptionText,
    bool CanApprovePrescription) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static PrescriptionReviewBoardState Initial { get; } = new(
        "处方审核 MVI",
        "待审 126 张",
        "等待药师录入审核意见",
        "药师录入处方、药品、剂量和过敏史，确认后联动库存扣减、补货计划和用药安全 MVI。",
        "等待处方审核录入。",
        "通过处方",
        "退回医生",
        true,
        true,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "提交处方审核",
        false);
}
