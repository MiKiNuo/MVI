using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心意图处理器。
/// </summary>
public sealed class ArchitectureValidationIntentHandler
    : IMviIntentHandler<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationMutation, ArchitectureValidationEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<ArchitectureValidationMutation, ArchitectureValidationEffect>> HandleAsync(
        ArchitectureValidationState state,
        ArchitectureValidationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<ArchitectureValidationMutation, ArchitectureValidationEffect> result = intent switch
        {
            ArchitectureValidationIntent.UpdateContext updateContext => HandleUpdateContext(updateContext),
            ArchitectureValidationIntent.AppendInteractionLog appendLog => HandleAppendInteractionLog(state, appendLog),
            _ => MviHandleResult.Empty<ArchitectureValidationMutation, ArchitectureValidationEffect>(),
        };

        return ValueTask.FromResult(result);
    }

    private static MviHandleResult<ArchitectureValidationMutation, ArchitectureValidationEffect> HandleUpdateContext(
        ArchitectureValidationIntent.UpdateContext intent)
    {
        return MviHandleResult.Mutations<ArchitectureValidationMutation, ArchitectureValidationEffect>(
            new ArchitectureValidationMutation.SetActiveContext(intent.ActiveContext),
            new ArchitectureValidationMutation.SetFlowStatus(intent.FlowStatus));
    }

    private static MviHandleResult<ArchitectureValidationMutation, ArchitectureValidationEffect> HandleAppendInteractionLog(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.AppendInteractionLog intent)
    {
        string nextLog = string.IsNullOrWhiteSpace(state.InteractionLog)
            ? intent.Message
            : state.InteractionLog + Environment.NewLine + intent.Message;

        return MviHandleResult.Mutations<ArchitectureValidationMutation, ArchitectureValidationEffect>(
            new ArchitectureValidationMutation.SetInteractionLog(nextLog));
    }
}
