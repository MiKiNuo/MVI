using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 页面级 ViewModel 工厂。
/// <para>
/// 由组合根（如 <c>SampleGeneratedContainer</c>）实现，把菜单选中的 PageKey 解析为对应的
/// 顶层页面 ViewModel（OutpatientWorkstation / BusinessCompositePage / ArchitectureValidation / ...）。
/// </para>
/// <para>
/// <see cref="DashboardViewModel"/> 持有此接口（不持有具体页面 ViewModel 引用），
/// 仅在用户切换菜单时通过 <see cref="DashboardViewModel.CreateCurrentPageViewModel"/>
/// 解析出目标页面 VM，避免 Parent VM 长期持有 Child VM 引用导致的"VM-in-VM"反模式。
/// </para>
/// </summary>
public interface IDashboardPageFactory
{
    /// <summary>
    /// 根据 <paramref name="pageKey"/> 创建对应的顶层页面 ViewModel。
    /// </summary>
    /// <param name="pageKey">菜单键（"门诊工作站"/"住院床位"/"检验医嘱"/...）。</param>
    /// <returns>页面 ViewModel 实例；未识别 pageKey 时返回 null。</returns>
    public object? CreatePage(string pageKey);
}
