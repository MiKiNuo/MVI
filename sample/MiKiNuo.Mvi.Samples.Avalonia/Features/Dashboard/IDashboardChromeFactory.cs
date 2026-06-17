using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳层 2 个常驻子组件 ViewModel（顶部头部 / 左侧菜单）的工厂。
/// <para>
/// 父 <see cref="DashboardViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="DashboardViewModel.CreateHeaderViewModel(string)"/>、
/// <see cref="DashboardViewModel.CreateMenuViewModel"/> 等方法按需解析。
/// </para>
/// <para>
/// 与 <see cref="IDashboardPageFactory"/> 区分：<see cref="IDashboardPageFactory"/> 负责可变
/// 顶层页面（OutpatientWorkstation / BusinessCompositePage / ArchitectureValidation / ...），
/// 本工厂负责"Shell 生命周期内静态不变"的菜单与头部 2 个 chrome 子组件。
/// </para>
/// </summary>
public interface IDashboardChromeFactory
{
    /// <summary>
    /// 解析顶部头部子组件 ViewModel。
    /// </summary>
    /// <param name="displayName">当前登录显示名称（用于头部欢迎语）。</param>
    /// <returns>头部 <see cref="HeaderViewModel"/> 实例（缓存）。</returns>
    public object CreateHeaderViewModel(string displayName);

    /// <summary>
    /// 解析左侧菜单子组件 ViewModel。
    /// </summary>
    /// <returns>菜单 <see cref="DashboardMenuViewModel"/> 实例（缓存）。</returns>
    public object CreateMenuViewModel();
}
