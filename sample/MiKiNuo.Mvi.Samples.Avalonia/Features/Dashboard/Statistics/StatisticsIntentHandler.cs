using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件意图处理器。
/// </summary>
public sealed class StatisticsIntentHandler
    : IMviIntentHandler<StatisticsState, StatisticsIntent, StatisticsMutation, StatisticsEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<StatisticsMutation, StatisticsEffect>> HandleAsync(
        StatisticsState state,
        StatisticsIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<StatisticsMutation, StatisticsEffect> result = intent switch
        {
            StatisticsIntent.Refresh refresh => HandleRefresh(refresh),
            _ => MviHandleResult.Empty<StatisticsMutation, StatisticsEffect>(),
        };
        return new ValueTask<MviHandleResult<StatisticsMutation, StatisticsEffect>>(result);
    }

    private static MviHandleResult<StatisticsMutation, StatisticsEffect> HandleRefresh(
        StatisticsIntent.Refresh intent)
    {
        return MviHandleResult.Mutations<StatisticsMutation, StatisticsEffect>(
            new StatisticsMutation.SetOnlineUsers(intent.OnlineUsers),
            new StatisticsMutation.SetRequestCount(intent.RequestCount),
            new StatisticsMutation.SetSuccessRate(intent.SuccessRate));
    }
}
