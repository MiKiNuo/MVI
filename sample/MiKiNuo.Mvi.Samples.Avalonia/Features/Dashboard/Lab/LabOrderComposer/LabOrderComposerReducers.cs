using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI 规约器。
/// </summary>
public static class LabOrderComposerReducers
{
    /// <summary>
    /// 处理患者标识变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">患者标识变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ChangePatientIdentity intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(RefreshFormState(state with { PatientIdentity = intent.PatientIdentity }));
    }

    /// <summary>
    /// 处理检验项目变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">检验项目变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ChangeTestItem intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(RefreshFormState(state with { TestItem = intent.TestItem }));
    }

    /// <summary>
    /// 处理优先级变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">优先级变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ChangePriorityLevel intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(RefreshFormState(state with { PriorityLevel = intent.PriorityLevel }));
    }

    /// <summary>
    /// 处理标本类型变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">标本类型变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ChangeSpecimenType intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(RefreshFormState(state with { SpecimenType = intent.SpecimenType }));
    }

    /// <summary>
    /// 处理临床指征变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">临床指征变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ChangeClinicalIndication intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(RefreshFormState(state with { ClinicalIndication = intent.ClinicalIndication }));
    }

    /// <summary>
    /// 处理提交检验医嘱意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">提交检验医嘱意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.SubmitLabOrderForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LabOrderComposerState refreshedState = RefreshFormState(state);
        if (!refreshedState.CanSubmitOrder)
        {
            return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(refreshedState with
            {
                ActionLog = "检验医嘱校验失败：患者标识、检验项目、标本类型为必填项。"
            });
        }

        string contextText = $"患者={refreshedState.PatientIdentity}；项目={refreshedState.TestItem}；优先级={refreshedState.PriorityLevel}；标本={refreshedState.SpecimenType}；指征={refreshedState.ClinicalIndication}";
        LabOrderComposerState nextState = refreshedState with
        {
            StatusText = "已提交检验医嘱，等待标本流转与 TAT 监控处理",
            ActionLog = $"医生提交检验医嘱 -> {contextText} -> 通过 Mediator 分发给标本、危急值和 TAT MVI。"
        };

        return MviReduceResult.StateAndEffect<LabOrderComposerState, LabOrderComposerEffect>(
            nextState,
            new LabOrderComposerEffect.RequestLabOrderSubmission(contextText));
    }

    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LabOrderComposerState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<LabOrderComposerState, LabOrderComposerEffect>(
            nextState,
            new LabOrderComposerEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LabOrderComposerState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<LabOrderComposerState, LabOrderComposerEffect>(
            nextState,
            new LabOrderComposerEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<LabOrderComposerState, LabOrderComposerEffect> Reduce(
        LabOrderComposerState state,
        LabOrderComposerIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabOrderComposerState, LabOrderComposerEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }

    private static LabOrderComposerState RefreshFormState(LabOrderComposerState state)
    {
        bool canSubmit = !string.IsNullOrWhiteSpace(state.PatientIdentity)
            && !string.IsNullOrWhiteSpace(state.TestItem)
            && !string.IsNullOrWhiteSpace(state.SpecimenType);

        return state with
        {
            CanSubmitOrder = canSubmit,
            StatusText = canSubmit ? "检验医嘱资料已完整，可以提交" : "请补齐患者标识、检验项目和标本类型",
            ActionLog = $"正在录入检验医嘱：患者={state.PatientIdentity}，项目={state.TestItem}，标本={state.SpecimenType}。"
        };
    }
}
