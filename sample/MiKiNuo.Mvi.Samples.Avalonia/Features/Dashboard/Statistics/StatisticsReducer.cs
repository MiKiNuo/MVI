using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件规约器。
/// </summary>
public sealed partial class StatisticsReducer
    : MviReducerBase<StatisticsState, StatisticsIntent, StatisticsEffect>
{
    /// <summary>
    /// 处理刷新统计意图。
    /// </summary>
    [MviReduce(typeof(StatisticsIntent.Refresh))]
    private MviReduceResult<StatisticsState, StatisticsEffect> HandleRefresh(
        StatisticsState state,
        StatisticsIntent.Refresh intent)
    {
        return MviReduceResult.State<StatisticsState, StatisticsEffect>(
            state with { OnlineUsers = intent.OnlineUsers, RequestCount = intent.RequestCount, SuccessRate = intent.SuccessRate });
    }
}
