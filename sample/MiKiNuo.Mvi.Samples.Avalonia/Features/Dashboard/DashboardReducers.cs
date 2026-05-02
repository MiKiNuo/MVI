using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳规约器。
/// </summary>
public static class DashboardReducers
{
    /// <summary>
    /// 处理显示业务页面意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">显示页面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<DashboardState, DashboardEffect> Reduce(
        DashboardState state,
        DashboardIntent.ShowPage intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<DashboardState, DashboardEffect>(state with
        {
            CurrentPageTitle = intent.PageTitle,
            CurrentPageDescription = intent.PageDescription,
            CurrentPageViewModel = intent.PageViewModel
        });
    }
}
