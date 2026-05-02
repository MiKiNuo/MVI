using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳意图。
/// </summary>
public abstract partial record DashboardIntent : IMviIntent
{
    /// <summary>
    /// 表示显示业务页面的意图。
    /// </summary>
    /// <param name="PageTitle">页面标题。</param>
    /// <param name="PageDescription">页面说明。</param>
    /// <param name="PageViewModel">页面 ViewModel。</param>
    public sealed partial record ShowPage(
        string PageTitle,
        string PageDescription,
        object PageViewModel) : DashboardIntent;
}
