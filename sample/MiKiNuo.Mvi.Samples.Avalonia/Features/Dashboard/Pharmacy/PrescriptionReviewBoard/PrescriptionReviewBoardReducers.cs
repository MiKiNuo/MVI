using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI 规约器。
/// </summary>
public static class PrescriptionReviewBoardReducers
{
    /// <summary>
    /// 处理处方号变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">处方号变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ChangePrescriptionNo intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(RefreshFormState(state with { PrescriptionNo = intent.PrescriptionNo }));
    }

    /// <summary>
    /// 处理患者姓名变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">患者姓名变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ChangePatientName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(RefreshFormState(state with { PatientName = intent.PatientName }));
    }

    /// <summary>
    /// 处理药品名称变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">药品名称变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ChangeDrugName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(RefreshFormState(state with { DrugName = intent.DrugName }));
    }

    /// <summary>
    /// 处理剂量用法变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">剂量用法变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ChangeDoseText intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(RefreshFormState(state with { DoseText = intent.DoseText }));
    }

    /// <summary>
    /// 处理过敏史变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">过敏史变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ChangeAllergyHistory intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(RefreshFormState(state with { AllergyHistory = intent.AllergyHistory }));
    }

    /// <summary>
    /// 处理提交处方审核意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">提交处方审核意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.SubmitPrescriptionReviewForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        PrescriptionReviewBoardState refreshedState = RefreshFormState(state);
        if (!refreshedState.CanApprovePrescription)
        {
            return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(refreshedState with
            {
                ActionLog = "处方审核校验失败：处方号、患者、药品和剂量为必填项。"
            });
        }

        string contextText = $"处方={refreshedState.PrescriptionNo}；患者={refreshedState.PatientName}；药品={refreshedState.DrugName}；剂量={refreshedState.DoseText}；过敏史={refreshedState.AllergyHistory}";
        PrescriptionReviewBoardState nextState = refreshedState with
        {
            StatusText = "已提交处方审核，等待库存、补货和用药安全组件处理",
            ActionLog = $"药师提交处方审核 -> {contextText} -> 通过 Mediator 分发给兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(
            nextState,
            new PrescriptionReviewBoardEffect.RequestPrescriptionReviewSubmission(contextText));
    }

    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        PrescriptionReviewBoardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(
            nextState,
            new PrescriptionReviewBoardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        PrescriptionReviewBoardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(
            nextState,
            new PrescriptionReviewBoardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect> Reduce(
        PrescriptionReviewBoardState state,
        PrescriptionReviewBoardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PrescriptionReviewBoardState, PrescriptionReviewBoardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }

    private static PrescriptionReviewBoardState RefreshFormState(PrescriptionReviewBoardState state)
    {
        bool canApprove = !string.IsNullOrWhiteSpace(state.PrescriptionNo)
            && !string.IsNullOrWhiteSpace(state.PatientName)
            && !string.IsNullOrWhiteSpace(state.DrugName)
            && !string.IsNullOrWhiteSpace(state.DoseText);

        return state with
        {
            CanApprovePrescription = canApprove,
            StatusText = canApprove ? "处方审核资料已完整，可以提交" : "请补齐处方号、患者、药品和剂量",
            ActionLog = $"正在录入处方审核：处方={state.PrescriptionNo}，药品={state.DrugName}，剂量={state.DoseText}。"
        };
    }
}
