using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI 副作用。
/// </summary>
public abstract partial record PrescriptionReviewBoardEffect : IMviEffect
{
    /// <summary>
    /// 表示请求提交处方审核副作用。
    /// </summary>
    /// <param name="ContextText">处方审核上下文。</param>
    public sealed partial record RequestPrescriptionReviewSubmission(string ContextText) : PrescriptionReviewBoardEffect;

    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : PrescriptionReviewBoardEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : PrescriptionReviewBoardEffect;
}
