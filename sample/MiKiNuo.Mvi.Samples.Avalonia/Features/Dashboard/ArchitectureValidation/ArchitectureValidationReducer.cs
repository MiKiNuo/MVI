using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心规约器。
/// </summary>
public sealed class ArchitectureValidationReducer
    : MviReducerBase<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> Reduce(
        ArchitectureValidationState state,
        ArchitectureValidationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            ArchitectureValidationIntent.UpdateContext updateContext => MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(
                state with { ActiveContext = updateContext.ActiveContext, FlowStatus = updateContext.FlowStatus }),
            ArchitectureValidationIntent.AppendInteractionLog appendLog => MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(
                state with { InteractionLog = ComputeNextLog(state.InteractionLog, appendLog.Message) }),
            _ => MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state),
        };
    }

    private static string ComputeNextLog(string currentLog, string message)
    {
        return string.IsNullOrWhiteSpace(currentLog)
            ? message
            : currentLog + Environment.NewLine + message;
    }
}
