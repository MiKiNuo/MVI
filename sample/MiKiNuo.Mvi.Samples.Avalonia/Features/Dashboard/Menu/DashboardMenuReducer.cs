using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单规约器。
/// </summary>
public sealed class DashboardMenuReducer
    : MviReducerBase<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<DashboardMenuState, DashboardMenuEffect> Reduce(
        DashboardMenuState state,
        DashboardMenuIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            DashboardMenuIntent.SelectMenuKey selectMenuKey
                when !string.Equals(state.SelectedMenuKey, selectMenuKey.SelectedMenuKey, StringComparison.Ordinal)
                => MviReduceResult.StateAndEffect<DashboardMenuState, DashboardMenuEffect>(
                    state with
                    {
                        SelectedMenuKey = selectMenuKey.SelectedMenuKey,
                        StatusText = $"正在通过 Mediator 切换到：{selectMenuKey.SelectedMenuKey}。",
                    },
                    new DashboardMenuEffect.RequestNavigation(selectMenuKey.SelectedMenuKey)),
            _ => MviReduceResult.State<DashboardMenuState, DashboardMenuEffect>(state),
        };
    }
}
