using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.MedicalRecordAuditBoard;

/// <summary>
/// 表示病历质控 MVI规约器。
/// </summary>
public static class MedicalRecordAuditBoardReducers
{
    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect> Reduce(
        MedicalRecordAuditBoardState state,
        MedicalRecordAuditBoardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MedicalRecordAuditBoardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect>(
            nextState,
            new MedicalRecordAuditBoardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect> Reduce(
        MedicalRecordAuditBoardState state,
        MedicalRecordAuditBoardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MedicalRecordAuditBoardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect>(
            nextState,
            new MedicalRecordAuditBoardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect> Reduce(
        MedicalRecordAuditBoardState state,
        MedicalRecordAuditBoardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<MedicalRecordAuditBoardState, MedicalRecordAuditBoardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }
}
