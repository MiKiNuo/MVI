using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件变更。
/// </summary>
public abstract record StatisticsMutation : IMviMutation<StatisticsState>
{
    /// <summary>
    /// 表示设置在线用户数的变更。
    /// </summary>
    /// <param name="Value">在线用户数。</param>
    [MviMutation(Path = "OnlineUsers")]
    public sealed record SetOnlineUsers(int Value) : StatisticsMutation;

    /// <summary>
    /// 表示设置请求数量的变更。
    /// </summary>
    /// <param name="Value">请求数量。</param>
    [MviMutation(Path = "RequestCount")]
    public sealed record SetRequestCount(int Value) : StatisticsMutation;

    /// <summary>
    /// 表示设置成功率的变更。
    /// </summary>
    /// <param name="Value">成功率。</param>
    [MviMutation(Path = "SuccessRate")]
    public sealed record SetSuccessRate(double Value) : StatisticsMutation;
}
