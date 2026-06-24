using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳变更。
/// </summary>
public abstract record DashboardMutation : IMviMutation<DashboardState>
{
    /// <summary>
    /// 表示设置当前页面键的变更。
    /// </summary>
    /// <param name="Value">页面键。</param>
    [MviMutation(Path = "CurrentPageKey")]
    public sealed record SetCurrentPageKey(string Value) : DashboardMutation;

    /// <summary>
    /// 表示设置当前页面标题的变更。
    /// </summary>
    /// <param name="Value">页面标题。</param>
    [MviMutation(Path = "CurrentPageTitle")]
    public sealed record SetCurrentPageTitle(string Value) : DashboardMutation;

    /// <summary>
    /// 表示设置当前页面说明的变更。
    /// </summary>
    /// <param name="Value">页面说明。</param>
    [MviMutation(Path = "CurrentPageDescription")]
    public sealed record SetCurrentPageDescription(string Value) : DashboardMutation;
}
