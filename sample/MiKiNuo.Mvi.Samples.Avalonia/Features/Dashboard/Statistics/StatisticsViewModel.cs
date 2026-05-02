using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Statistics;

/// <summary>
/// 表示统计组件 ViewModel。
/// </summary>
public sealed partial class StatisticsViewModel
    : MviViewModelBase<StatisticsState, StatisticsIntent, StatisticsEffect>
{
    /// <summary>
    /// 初始化统计组件 ViewModel。
    /// </summary>
    /// <param name="store">统计组件状态存储。</param>
    public StatisticsViewModel(IMviStore<StatisticsState, StatisticsIntent, StatisticsEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取在线用户数。
    /// </summary>
    [MviBind(nameof(StatisticsState.OnlineUsers))]
    public partial int OnlineUsers { get; private set; }

    /// <summary>
    /// 获取请求数量。
    /// </summary>
    [MviBind(nameof(StatisticsState.RequestCount))]
    public partial int RequestCount { get; private set; }

    /// <summary>
    /// 获取成功率。
    /// </summary>
    [MviBind(nameof(StatisticsState.SuccessRate))]
    public partial double SuccessRate { get; private set; }
}
