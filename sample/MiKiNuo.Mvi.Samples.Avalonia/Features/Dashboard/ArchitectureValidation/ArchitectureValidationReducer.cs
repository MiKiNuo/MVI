using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心规约器。
/// </summary>
public sealed partial class ArchitectureValidationReducer
    : MviReducerBase<ArchitectureValidationState, ArchitectureValidationIntent, UnitEffect>
{
    /// <summary>
    /// 处理上下文更新意图。
    /// </summary>
    [MviReduce(typeof(ArchitectureValidationIntent.UpdateContext))]
    private MviReduceResult<ArchitectureValidationState, UnitEffect> HandleUpdateContext(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.UpdateContext intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<ArchitectureValidationState, UnitEffect>(
            state with { ActiveContext = intent.ActiveContext, FlowStatus = intent.FlowStatus });
    }

    /// <summary>
    /// 处理追加日志意图。
    /// </summary>
    [MviReduce(typeof(ArchitectureValidationIntent.AppendInteractionLog))]
    private MviReduceResult<ArchitectureValidationState, UnitEffect> HandleAppendInteractionLog(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.AppendInteractionLog intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<ArchitectureValidationState, UnitEffect>(
            state with { InteractionLog = ComputeNextLog(state.InteractionLog, intent.Message) });
    }

    private string ComputeNextLog(string currentLog, string message)
    {
        return string.IsNullOrWhiteSpace(currentLog)
            ? message
            : currentLog + Environment.NewLine + message;
    }
}
