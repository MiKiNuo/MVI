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
    /// 处理刷新统计数据意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">刷新统计数据意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<StatisticsState, StatisticsEffect> Reduce(
        StatisticsState state,
        StatisticsIntent.Refresh intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<StatisticsState, StatisticsEffect>(state with
        {
            OnlineUsers = intent.OnlineUsers,
            RequestCount = intent.RequestCount,
            SuccessRate = intent.SuccessRate
        });
    }
}
