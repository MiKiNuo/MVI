using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心变更规约器。
/// </summary>
public sealed partial class ArchitectureValidationMutationReducer
    : MviMutationReducerBase<ArchitectureValidationState, ArchitectureValidationMutation, ArchitectureValidationEffect>
{
    /// <summary>
    /// 应用设置当前业务上下文变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> HandleSetActiveContext(
        ArchitectureValidationState state,
        ArchitectureValidationMutation.SetActiveContext mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置当前流程状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> HandleSetFlowStatus(
        ArchitectureValidationState state,
        ArchitectureValidationMutation.SetFlowStatus mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置交互日志变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> HandleSetInteractionLog(
        ArchitectureValidationState state,
        ArchitectureValidationMutation.SetInteractionLog mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state.Apply(mutation));
    }
}
