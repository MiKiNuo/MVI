using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单意图。
/// </summary>
public abstract partial record DashboardMenuIntent : IMviIntent
{
    /// <summary>
    /// 表示选择菜单项意图。
    /// </summary>
    /// <param name="SelectedMenuKey">选中的菜单键。</param>
    public sealed partial record SelectMenuKey(string SelectedMenuKey) : DashboardMenuIntent;
}
