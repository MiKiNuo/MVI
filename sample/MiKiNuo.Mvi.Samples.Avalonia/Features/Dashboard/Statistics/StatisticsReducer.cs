using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件规约器。
/// </summary>
public sealed class StatisticsReducer
    : MviReducerBase<StatisticsState, StatisticsIntent, StatisticsEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<StatisticsState, StatisticsEffect> Reduce(
        StatisticsState state,
        StatisticsIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            StatisticsIntent.Refresh refresh => MviReduceResult.State<StatisticsState, StatisticsEffect>(
                state with { OnlineUsers = refresh.OnlineUsers, RequestCount = refresh.RequestCount, SuccessRate = refresh.SuccessRate }),
            _ => MviReduceResult.State<StatisticsState, StatisticsEffect>(state),
        };
    }
}
