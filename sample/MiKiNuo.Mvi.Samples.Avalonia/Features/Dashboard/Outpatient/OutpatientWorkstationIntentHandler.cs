using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面意图处理器。
/// </summary>
public sealed class OutpatientWorkstationIntentHandler
    : IMviIntentHandler<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationMutation, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<OutpatientWorkstationMutation, OutpatientWorkstationEffect>> HandleAsync(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return new ValueTask<MviHandleResult<OutpatientWorkstationMutation, OutpatientWorkstationEffect>>(
            MviHandleResult.Empty<OutpatientWorkstationMutation, OutpatientWorkstationEffect>());
    }
}
