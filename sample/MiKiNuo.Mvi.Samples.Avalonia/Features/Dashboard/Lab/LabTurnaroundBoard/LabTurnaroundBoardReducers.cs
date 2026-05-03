using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabTurnaroundBoard;

/// <summary>
/// 表示TAT 监控 MVI规约器。
/// </summary>
public sealed partial class LabTurnaroundBoardReducer
    : MviReducerBase<LabTurnaroundBoardState, LabTurnaroundBoardIntent, LabTurnaroundBoardEffect>
{
    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LabTurnaroundBoardState, LabTurnaroundBoardEffect> Reduce(
        LabTurnaroundBoardState state,
        LabTurnaroundBoardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LabTurnaroundBoardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<LabTurnaroundBoardState, LabTurnaroundBoardEffect>(
            nextState,
            new LabTurnaroundBoardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LabTurnaroundBoardState, LabTurnaroundBoardEffect> Reduce(
        LabTurnaroundBoardState state,
        LabTurnaroundBoardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LabTurnaroundBoardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<LabTurnaroundBoardState, LabTurnaroundBoardEffect>(
            nextState,
            new LabTurnaroundBoardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LabTurnaroundBoardState, LabTurnaroundBoardEffect> Reduce(
        LabTurnaroundBoardState state,
        LabTurnaroundBoardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<LabTurnaroundBoardState, LabTurnaroundBoardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }
}
