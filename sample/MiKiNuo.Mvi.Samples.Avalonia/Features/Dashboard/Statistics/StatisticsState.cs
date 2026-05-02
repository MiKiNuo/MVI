using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件状态。
/// </summary>
/// <param name="OnlineUsers">在线用户数。</param>
/// <param name="RequestCount">请求数量。</param>
/// <param name="SuccessRate">成功率。</param>
public sealed record StatisticsState(int OnlineUsers, int RequestCount, double SuccessRate) : IMviState;
