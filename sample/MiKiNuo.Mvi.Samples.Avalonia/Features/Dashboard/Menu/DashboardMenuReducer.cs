using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单规约器。
/// </summary>
public sealed partial class DashboardMenuReducer
    : MviReducerBase<DashboardMenuState, DashboardMenuIntent, DashboardMenuEffect>
{
    /// <summary>
    /// 处理选择菜单项意图。
    /// </summary>
    [MviReduce(typeof(DashboardMenuIntent.SelectMenuKey))]
    private MviReduceResult<DashboardMenuState, DashboardMenuEffect> HandleSelectMenuKey(
        DashboardMenuState state,
        DashboardMenuIntent.SelectMenuKey intent,
        IMviBusinessResult? result)
    {
        if (string.Equals(state.SelectedMenuKey, intent.SelectedMenuKey, StringComparison.Ordinal))
        {
            return Unchanged(state);
        }

        return WithEffect(
            state with
            {
                SelectedMenuKey = intent.SelectedMenuKey,
                StatusText = $"正在通过 Mediator 切换到：{intent.SelectedMenuKey}。",
            },
            new DashboardMenuEffect.RequestNavigation(intent.SelectedMenuKey));
    }
}
