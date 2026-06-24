using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件变更规约器。
/// </summary>
public sealed partial class StatisticsMutationReducer
    : MviMutationReducerBase<StatisticsState, StatisticsMutation, StatisticsEffect>
{
    /// <summary>
    /// 应用设置在线用户数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<StatisticsState, StatisticsEffect> HandleSetOnlineUsers(
        StatisticsState state,
        StatisticsMutation.SetOnlineUsers mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<StatisticsState, StatisticsEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置请求数量变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<StatisticsState, StatisticsEffect> HandleSetRequestCount(
        StatisticsState state,
        StatisticsMutation.SetRequestCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<StatisticsState, StatisticsEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置成功率变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<StatisticsState, StatisticsEffect> HandleSetSuccessRate(
        StatisticsState state,
        StatisticsMutation.SetSuccessRate mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<StatisticsState, StatisticsEffect>(state.Apply(mutation));
    }
}
