using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳变更规约器。
/// </summary>
public sealed partial class AppShellMutationReducer
    : MviMutationReducerBase<AppShellState, AppShellMutation, AppShellEffect>
{
    /// <summary>
    /// 应用设置当前页面键变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AppShellState, AppShellEffect> HandleSetCurrentPageKey(
        AppShellState state,
        AppShellMutation.SetCurrentPageKey mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AppShellState, AppShellEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置页面标题变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AppShellState, AppShellEffect> HandleSetTitle(
        AppShellState state,
        AppShellMutation.SetTitle mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AppShellState, AppShellEffect>(state.Apply(mutation));
    }
}
