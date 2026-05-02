using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.ViewRegistry;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 组合视图。
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
        _headerSlot.Content = _viewRegistry.CreateView(viewModel.HeaderViewModel);
        _menuSlot.Content = _viewRegistry.CreateView(viewModel.MenuViewModel);
        RenderPage(viewModel.CurrentPageViewModel);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DashboardViewModel.CurrentPageViewModel))
            {
                RenderPage(viewModel.CurrentPageViewModel);
            }
        };
    }

    private void RenderPage(object pageViewModel)
    {
        _pageSlot.Content = _viewRegistry.CreateView(pageViewModel);
    }
}
