using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI 规约器。
/// </summary>
public sealed partial class RiskEventBoardReducer
    : MviReducerBase<RiskEventBoardState, RiskEventBoardIntent, RiskEventBoardEffect>
{
    /// <summary>
    /// 处理事件标题变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">事件标题变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ChangeEventTitle intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(RefreshFormState(state with { EventTitle = intent.EventTitle }));
    }

    /// <summary>
    /// 处理责任科室变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">责任科室变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ChangeDepartmentName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(RefreshFormState(state with { DepartmentName = intent.DepartmentName }));
    }

    /// <summary>
    /// 处理风险等级变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">风险等级变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ChangeSeverityLevel intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(RefreshFormState(state with { SeverityLevel = intent.SeverityLevel }));
    }

    /// <summary>
    /// 处理责任人变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">责任人变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ChangeResponsibleOwner intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(RefreshFormState(state with { ResponsibleOwner = intent.ResponsibleOwner }));
    }

    /// <summary>
    /// 处理整改措施变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">整改措施变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ChangeCorrectiveAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(RefreshFormState(state with { CorrectiveAction = intent.CorrectiveAction }));
    }

    /// <summary>
    /// 处理提交风险事件意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">提交风险事件意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.SubmitRiskEventForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        RiskEventBoardState refreshedState = RefreshFormState(state);
        if (!refreshedState.CanSubmitRiskEvent)
        {
            return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(refreshedState with
            {
                ActionLog = "风险事件校验失败：事件标题、责任科室、风险等级和整改措施为必填项。"
            });
        }

        string contextText = $"事件={refreshedState.EventTitle}；科室={refreshedState.DepartmentName}；等级={refreshedState.SeverityLevel}；责任人={refreshedState.ResponsibleOwner}；整改={refreshedState.CorrectiveAction}";
        RiskEventBoardState nextState = refreshedState with
        {
            StatusText = "已提交风险事件，等待 KPI、病历质控和整改闭环组件处理",
            ActionLog = $"质控员提交风险事件 -> {contextText} -> 通过 Mediator 分发给兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<RiskEventBoardState, RiskEventBoardEffect>(
            nextState,
            new RiskEventBoardEffect.RequestRiskEventSubmission(contextText));
    }

    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        RiskEventBoardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<RiskEventBoardState, RiskEventBoardEffect>(
            nextState,
            new RiskEventBoardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        RiskEventBoardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<RiskEventBoardState, RiskEventBoardEffect>(
            nextState,
            new RiskEventBoardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<RiskEventBoardState, RiskEventBoardEffect> Reduce(
        RiskEventBoardState state,
        RiskEventBoardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<RiskEventBoardState, RiskEventBoardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }

    private static RiskEventBoardState RefreshFormState(RiskEventBoardState state)
    {
        bool canSubmit = !string.IsNullOrWhiteSpace(state.EventTitle)
            && !string.IsNullOrWhiteSpace(state.DepartmentName)
            && !string.IsNullOrWhiteSpace(state.SeverityLevel)
            && !string.IsNullOrWhiteSpace(state.CorrectiveAction);

        return state with
        {
            CanSubmitRiskEvent = canSubmit,
            StatusText = canSubmit ? "风险事件资料已完整，可以提交" : "请补齐事件标题、责任科室、风险等级和整改措施",
            ActionLog = $"正在录入风险事件：事件={state.EventTitle}，科室={state.DepartmentName}，等级={state.SeverityLevel}。"
        };
    }
}
