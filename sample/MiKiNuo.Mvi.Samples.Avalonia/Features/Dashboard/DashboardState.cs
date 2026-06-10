using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳状态。
/// <para>
/// 子组件 ViewModel（菜单、头部、当前页面）不进入 State：菜单/头部为 Shell 生命周期内的静态装配
/// （通过 <see cref="DashboardViewModel"/> 构造函数注入），当前页面为动态切换
/// （State 仅保存 PageKey 判别器 + 标题/说明，View 层通过 <see cref="IDashboardPageFactory"/>
/// 把 PageKey 解析为具体页面 VM）。
/// </para>
/// </summary>
/// <param name="DisplayName">当前登录显示名称。</param>
/// <param name="CurrentPageKey">当前业务页面键（菜单驱动）。</param>
/// <param name="CurrentPageTitle">当前业务页面标题。</param>
/// <param name="CurrentPageDescription">当前业务页面说明。</param>
public sealed record DashboardState(
    string DisplayName,
    string CurrentPageKey,
    string CurrentPageTitle,
    string CurrentPageDescription) : IMviState;
