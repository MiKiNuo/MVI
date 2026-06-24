using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳变更规约器。
/// </summary>
public sealed partial class DashboardMutationReducer
    : MviMutationReducerBase<DashboardState, DashboardMutation, DashboardEffect>
{
    /// <summary>
    /// 应用设置当前页面键变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<DashboardState, DashboardEffect> HandleSetCurrentPageKey(
        DashboardState state,
        DashboardMutation.SetCurrentPageKey mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<DashboardState, DashboardEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置当前页面标题变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<DashboardState, DashboardEffect> HandleSetCurrentPageTitle(
        DashboardState state,
        DashboardMutation.SetCurrentPageTitle mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<DashboardState, DashboardEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置当前页面说明变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<DashboardState, DashboardEffect> HandleSetCurrentPageDescription(
        DashboardState state,
        DashboardMutation.SetCurrentPageDescription mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<DashboardState, DashboardEffect>(state.Apply(mutation));
    }
}
