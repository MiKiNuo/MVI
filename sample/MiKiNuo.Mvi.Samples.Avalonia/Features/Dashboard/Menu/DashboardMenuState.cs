using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 左侧菜单项的描述。
/// 用于在 XAML 中以「分组 + 图标 + 选中态」的结构化方式渲染菜单。
/// </summary>
/// <param name="Key">菜单键（与原 SelectedMenuKey 保持向后兼容）。</param>
/// <param name="Label">显示名。</param>
/// <param name="Group">所属分组标题（用于分组显示）。</param>
/// <param name="IconKey">图标键，XAML 端通过转换器映射到具体图标资源（医院/试管/药箱/雷达等）。</param>
/// <param name="DomainKey">业务域键：Inpatient/Lab/Pharmacy/Quality/Other。XAML 端用于读取对应颜色刷。</param>
/// <param name="Description">选中后展示在右侧详情区的业务说明。</param>
/// <param name="CardCount">该菜单页包含的卡片数（用于底部状态提示）。</param>
public sealed record DashboardMenuItemDescriptor(
    string Key,
    string Label,
    string Group,
    string IconKey,
    string DomainKey,
    string Description,
    int CardCount);

/// <summary>
/// 表示 Dashboard 左侧菜单状态。
/// </summary>
/// <param name="MenuItems">菜单项集合（按展示顺序）。</param>
/// <param name="Groups">分组标题集合（按展示顺序）。</param>
/// <param name="SelectedMenuKey">当前选中的菜单键。</param>
/// <param name="StatusText">菜单状态文本。</param>
public sealed record DashboardMenuState(
    IReadOnlyList<DashboardMenuItemDescriptor> MenuItems,
    IReadOnlyList<string> Groups,
    string SelectedMenuKey,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取默认 HIS 菜单状态。
    /// 6 个菜单项按 4 个业务域分组；CardCount 取自 DashboardCardRegistry（4 卡/域）+ 2 个独立菜单（门诊/架构）= 4+4+4+4+3+1=20。
    /// </summary>
    public static DashboardMenuState Initial { get; } = new(
        MenuItems:
        [
            new DashboardMenuItemDescriptor(
                "门诊工作站",
                "门诊工作站",
                "门诊业务",
                "icon-outpatient",
                "Other",
                "门诊医生工作站：队列、提醒、临床文档一体化编辑。",
                3),
            new DashboardMenuItemDescriptor(
                "住院床位",
                "住院床位",
                "住院业务",
                "icon-inpatient",
                "Inpatient",
                "床位调度 / 入院登记 / 护理任务 / 病区风险 4 卡片数据协同。",
                4),
            new DashboardMenuItemDescriptor(
                "检验医嘱",
                "检验医嘱",
                "诊断业务",
                "icon-lab",
                "Lab",
                "医嘱开立 / 标本流转 / 危急值 / TAT 监控 4 卡片流水线协同。",
                4),
            new DashboardMenuItemDescriptor(
                "药房库存",
                "药房库存",
                "药品业务",
                "icon-pharmacy",
                "Pharmacy",
                "处方审核 / 库存水位 / 补货计划 / 用药安全 4 卡片联动。",
                4),
            new DashboardMenuItemDescriptor(
                "运营质控",
                "运营质控",
                "质量管理",
                "icon-quality",
                "Quality",
                "院级 KPI / 病历缺陷 / 风险事件 / 整改闭环 4 卡片闭环追踪。",
                4),
            new DashboardMenuItemDescriptor(
                "架构验证中心",
                "架构验证中心",
                "平台工具",
                "icon-arch",
                "Other",
                "演示分层架构校验、依赖方向测试与代码健康度检查。",
                1),
        ],
        Groups: ["门诊业务", "住院业务", "诊断业务", "药品业务", "质量管理", "平台工具"],
        SelectedMenuKey: "住院床位",
        StatusText: "通过 Mediator 切换业务页面，点击卡片按钮触发组合式 MVI 数据流。");
}
