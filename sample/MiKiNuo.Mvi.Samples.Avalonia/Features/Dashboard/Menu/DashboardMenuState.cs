using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 左侧菜单状态。
/// </summary>
/// <param name="MenuItems">菜单项集合。</param>
/// <param name="SelectedMenuKey">当前选中的菜单键。</param>
/// <param name="StatusText">菜单状态文本。</param>
public sealed record DashboardMenuState(
    IReadOnlyList<string> MenuItems,
    string SelectedMenuKey,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取默认 HIS 菜单状态。
    /// </summary>
    public static DashboardMenuState Initial { get; } = new(
        ["门诊工作站", "住院床位", "检验医嘱", "药房库存", "运营质控", "架构验证中心"],
        "门诊工作站",
        "通过真正 Mediator 请求 Dashboard 切换页面。");
}
