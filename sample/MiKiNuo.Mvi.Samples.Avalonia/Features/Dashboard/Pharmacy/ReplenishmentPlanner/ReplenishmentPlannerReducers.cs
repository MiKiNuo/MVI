using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.ReplenishmentPlanner;

/// <summary>
/// 表示补货计划 MVI规约器。
/// </summary>
public sealed partial class ReplenishmentPlannerReducer
    : MviReducerBase<ReplenishmentPlannerState, ReplenishmentPlannerIntent, ReplenishmentPlannerEffect>
{
    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ReplenishmentPlannerState, ReplenishmentPlannerEffect> Reduce(
        ReplenishmentPlannerState state,
        ReplenishmentPlannerIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        ReplenishmentPlannerState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<ReplenishmentPlannerState, ReplenishmentPlannerEffect>(
            nextState,
            new ReplenishmentPlannerEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ReplenishmentPlannerState, ReplenishmentPlannerEffect> Reduce(
        ReplenishmentPlannerState state,
        ReplenishmentPlannerIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        ReplenishmentPlannerState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<ReplenishmentPlannerState, ReplenishmentPlannerEffect>(
            nextState,
            new ReplenishmentPlannerEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ReplenishmentPlannerState, ReplenishmentPlannerEffect> Reduce(
        ReplenishmentPlannerState state,
        ReplenishmentPlannerIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ReplenishmentPlannerState, ReplenishmentPlannerEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }
}
