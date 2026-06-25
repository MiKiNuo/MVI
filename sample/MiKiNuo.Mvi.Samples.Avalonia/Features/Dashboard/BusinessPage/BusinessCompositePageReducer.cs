using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面规约器。
/// </summary>
public sealed class BusinessCompositePageReducer
    : MviReducerBase<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<BusinessCompositePageState, BusinessCompositePageEffect> Reduce(
        BusinessCompositePageState state,
        BusinessCompositePageIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            BusinessCompositePageIntent.UpdateContext updateContext => MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(
                state with { ActiveContext = updateContext.ActiveContext, FlowStatus = updateContext.FlowStatus }),
            BusinessCompositePageIntent.AppendInteractionLog appendLog => MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(
                state with { InteractionLog = ComputeNextLog(state.InteractionLog, appendLog.Message) }),
            _ => MviReduceResult.State<BusinessCompositePageState, BusinessCompositePageEffect>(state),
        };
    }

    private static string ComputeNextLog(string currentLog, string message)
    {
        return string.IsNullOrWhiteSpace(currentLog)
            ? message
            : message + "\n" + currentLog;
    }
}
