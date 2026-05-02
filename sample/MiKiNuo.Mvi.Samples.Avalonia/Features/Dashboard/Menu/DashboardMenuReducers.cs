using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单规约器。
/// </summary>
public static class DashboardMenuReducers
{
    /// <summary>
    /// 处理选择菜单项意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">选择菜单意图。</param>
    /// <returns>规约结果。</returns>
    [MviReducer]
    public static MviReduceResult<DashboardMenuState, DashboardMenuEffect> Reduce(
        DashboardMenuState state,
        DashboardMenuIntent.SelectMenuKey intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (string.Equals(state.SelectedMenuKey, intent.SelectedMenuKey, StringComparison.Ordinal))
        {
            return MviReduceResult.State<DashboardMenuState, DashboardMenuEffect>(state);
        }

        DashboardMenuState nextState = state with
        {
            SelectedMenuKey = intent.SelectedMenuKey,
            StatusText = $"正在通过 Mediator 切换到：{intent.SelectedMenuKey}。"
        };

        return MviReduceResult.StateAndEffect<DashboardMenuState, DashboardMenuEffect>(
            nextState,
            new DashboardMenuEffect.RequestNavigation(intent.SelectedMenuKey));
    }
}
