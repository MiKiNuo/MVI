using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳规约器。
/// </summary>
public sealed class DashboardReducer
    : MviReducerBase<DashboardState, DashboardIntent, DashboardEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<DashboardState, DashboardEffect> Reduce(
        DashboardState state,
        DashboardIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            DashboardIntent.ShowPage showPage => MviReduceResult.State<DashboardState, DashboardEffect>(
                state with
                {
                    CurrentPageKey = showPage.PageKey,
                    CurrentPageTitle = showPage.PageTitle,
                    CurrentPageDescription = showPage.PageDescription,
                }),
            _ => MviReduceResult.State<DashboardState, DashboardEffect>(state),
        };
    }
}
