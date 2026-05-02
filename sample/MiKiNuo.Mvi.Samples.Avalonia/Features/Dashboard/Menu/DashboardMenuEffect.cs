using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单副作用。
/// </summary>
public abstract partial record DashboardMenuEffect : IMviEffect
{
    /// <summary>
    /// 表示请求 Dashboard 页面导航副作用。
    /// </summary>
    /// <param name="PageKey">页面键。</param>
    public sealed partial record RequestNavigation(string PageKey) : DashboardMenuEffect;
}
