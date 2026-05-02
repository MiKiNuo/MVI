using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳状态。
/// </summary>
/// <param name="DisplayName">当前登录显示名称。</param>
/// <param name="MenuViewModel">左侧菜单 ViewModel。</param>
/// <param name="HeaderViewModel">顶部头部 ViewModel。</param>
/// <param name="CurrentPageTitle">当前业务页面标题。</param>
/// <param name="CurrentPageDescription">当前业务页面说明。</param>
/// <param name="CurrentPageViewModel">当前业务页面 ViewModel。</param>
public sealed record DashboardState(
    string DisplayName,
    object MenuViewModel,
    object HeaderViewModel,
    string CurrentPageTitle,
    string CurrentPageDescription,
    object CurrentPageViewModel) : IMviState;
