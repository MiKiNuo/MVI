using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单变更。
/// </summary>
public abstract record DashboardMenuMutation : IMviMutation<DashboardMenuState>
{
    /// <summary>
    /// 表示设置选中菜单键的变更。
    /// </summary>
    /// <param name="Value">菜单键。</param>
    [MviMutation(Path = "SelectedMenuKey")]
    public sealed record SetSelectedMenuKey(string Value) : DashboardMenuMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : DashboardMenuMutation;
}
