using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单变更规约器。
/// </summary>
public sealed partial class DashboardMenuMutationReducer
    : MviMutationReducerBase<DashboardMenuState, DashboardMenuMutation, DashboardMenuEffect>
{
    /// <summary>
    /// 应用设置选中菜单键变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<DashboardMenuState, DashboardMenuEffect> HandleSetSelectedMenuKey(
        DashboardMenuState state,
        DashboardMenuMutation.SetSelectedMenuKey mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<DashboardMenuState, DashboardMenuEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置状态文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<DashboardMenuState, DashboardMenuEffect> HandleSetStatusText(
        DashboardMenuState state,
        DashboardMenuMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<DashboardMenuState, DashboardMenuEffect>(state.Apply(mutation));
    }
}
