using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心规约器。
/// </summary>
public sealed partial class ArchitectureValidationReducer
    : MviReducerBase<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect>
{
    /// <summary>
    /// 处理上下文更新意图。
    /// </summary>
    [MviReduce(typeof(ArchitectureValidationIntent.UpdateContext))]
    private static MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> HandleUpdateContext(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.UpdateContext intent)
    {
        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(
            state with { ActiveContext = intent.ActiveContext, FlowStatus = intent.FlowStatus });
    }

    /// <summary>
    /// 处理追加日志意图。
    /// </summary>
    [MviReduce(typeof(ArchitectureValidationIntent.AppendInteractionLog))]
    private static MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> HandleAppendInteractionLog(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.AppendInteractionLog intent)
    {
        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(
            state with { InteractionLog = ComputeNextLog(state.InteractionLog, intent.Message) });
    }

    private static string ComputeNextLog(string currentLog, string message)
    {
        return string.IsNullOrWhiteSpace(currentLog)
            ? message
            : currentLog + Environment.NewLine + message;
    }
}
