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
    /// 处理更新当前架构验证上下文意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">更新上下文意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> Reduce(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.UpdateContext intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state with
        {
            ActiveContext = intent.ActiveContext,
            FlowStatus = intent.FlowStatus
        });
    }

    /// <summary>
    /// 处理追加交互日志意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">追加交互日志意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<ArchitectureValidationState, ArchitectureValidationEffect> Reduce(
        ArchitectureValidationState state,
        ArchitectureValidationIntent.AppendInteractionLog intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<ArchitectureValidationState, ArchitectureValidationEffect>(state with
        {
            InteractionLog = string.IsNullOrWhiteSpace(state.InteractionLog)
                ? intent.Message
                : state.InteractionLog + Environment.NewLine + intent.Message
        });
    }
}
