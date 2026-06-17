using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 组合视图。
/// <para>
/// 头部 / 菜单 / 当前页面 3 个槽位均使用 <c>[MviSlot]</c> 字段声明，
/// 由 <c>MviCompositeSlotBindingGenerator</c> 在编译期 emit <c>OnBindSlots</c> override：
/// </para>
/// <list type="bullet">
/// <item>头部 / 菜单：通过 <see cref="DashboardViewModel"/> 的 <c>CreateHeaderViewModel(string)</c>
///   / <c>CreateMenuViewModel()</c> 工厂按需解析，<b>不</b>订阅属性变化（一次性绑定）。</item>
/// <item>当前页面：通过 <c>CreateCurrentPageViewModel()</c> 解析，并订阅 <see cref="DashboardViewModel.CurrentPageKey"/>
///   属性变化时重新解析——菜单切换会触发重渲。</item>
/// </list>
/// <para>
/// 视图不再持有 <see cref="IMviViewRegistry"/> 引用：源生成器从 <c>Bind(viewModel, resolver)</c>
///   注入的 <see cref="IMviResolver"/> 中解析 <see cref="IMviViewRegistry"/>，避免把"服务定位器"透传到 View。
/// </para>
/// </summary>
public sealed partial class DashboardView : MviAvaloniaView<DashboardViewModel>
{
    /// <summary>
    /// 头部槽位：常驻子 View，绑定时通过 <c>CreateHeaderViewModel(displayName)</c> 解析。
    /// </summary>
    [MviSlot(typeof(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header.HeaderView), factory: nameof(DashboardViewModel.CreateHeaderViewModel))]
    private MviSlotHost? _headerSlot;

    /// <summary>
    /// 菜单槽位：常驻子 View，绑定时通过 <c>CreateMenuViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu.DashboardMenuView), factory: nameof(DashboardViewModel.CreateMenuViewModel))]
    private MviSlotHost? _menuSlot;

    /// <summary>
    /// 当前页面槽位：随 <see cref="DashboardViewModel.CurrentPageKey"/> 变化重新解析。
    /// </summary>
    [MviSlot(
        typeof(object),
        factory: nameof(DashboardViewModel.CreateCurrentPageViewModel),
        nameof(DashboardViewModel.CurrentPageKey))]
    private MviSlotHost? _pageSlot;

    /// <summary>
    /// 初始化 Dashboard 组合视图。
    /// </summary>
    public DashboardView()
    {
        AvaloniaXamlLoader.Load(this);
        _headerSlot = this.FindControl<MviSlotHost>("HeaderSlot")
            ?? throw new InvalidOperationException("无法找到 HeaderSlot。");
        _menuSlot = this.FindControl<MviSlotHost>("MenuSlot")
            ?? throw new InvalidOperationException("无法找到 MenuSlot。");
        _pageSlot = this.FindControl<MviSlotHost>("PageSlot")
            ?? throw new InvalidOperationException("无法找到 PageSlot。");
    }
}
