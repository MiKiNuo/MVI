using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 组合视图。
/// 菜单 / 头部插槽在绑定时通过 <see cref="DashboardViewModel"/> 工厂方法按需解析（它们是 Shell 生命周期内静态注入的）。
/// 当前页面插槽通过 <see cref="DashboardViewModel.CurrentPageKey"/> 变更触发重新渲染，
/// View 不再从 ViewModel 拉取可变 child VM 引用。
/// </summary>
public sealed partial class DashboardView : MviAvaloniaView<DashboardViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly MviSlotHost _headerSlot;
    private readonly MviSlotHost _menuSlot;
    private readonly MviSlotHost _pageSlot;

    /// <summary>
    /// 初始化 Dashboard 组合视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public DashboardView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
        _headerSlot = this.FindControl<MviSlotHost>("HeaderSlot")
            ?? throw new InvalidOperationException("无法找到 HeaderSlot。");
        _menuSlot = this.FindControl<MviSlotHost>("MenuSlot")
            ?? throw new InvalidOperationException("无法找到 MenuSlot。");
        _pageSlot = this.FindControl<MviSlotHost>("PageSlot")
            ?? throw new InvalidOperationException("无法找到 PageSlot。");
    }

    /// <inheritdoc />
    public new void Bind(DashboardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        _headerSlot.Content = _viewRegistry.CreateView(viewModel.CreateHeaderViewModel(viewModel.DisplayName));
        _menuSlot.Content = _viewRegistry.CreateView(viewModel.CreateMenuViewModel());
        RenderPage(viewModel);
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName == nameof(DashboardViewModel.CurrentPageKey))
            {
                RenderPage(viewModel);
            }
        };
        viewModel.PropertyChanged += handler;
        RegisterBinding(() => viewModel.PropertyChanged -= handler);
    }

    private void RenderPage(DashboardViewModel viewModel)
    {
        object? pageViewModel = viewModel.CreateCurrentPageViewModel();
        _pageSlot.Content = pageViewModel is null ? null : _viewRegistry.CreateView(pageViewModel);
    }
}
