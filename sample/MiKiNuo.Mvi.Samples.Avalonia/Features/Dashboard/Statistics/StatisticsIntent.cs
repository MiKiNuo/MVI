using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件意图。
/// </summary>
public abstract partial record StatisticsIntent : IMviIntent
{
    /// <summary>
    /// 表示刷新统计数据意图。
    /// </summary>
    /// <param name="OnlineUsers">在线用户数。</param>
    /// <param name="RequestCount">请求数量。</param>
    /// <param name="SuccessRate">成功率。</param>
    public sealed partial record Refresh(int OnlineUsers, int RequestCount, double SuccessRate) : StatisticsIntent;
}
