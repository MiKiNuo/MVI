using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定复杂组合根视图。
/// <para>
/// 3 个子组件 ViewModel 由 <see cref="EventBindingWorkbenchViewModel"/> 工厂方法按需解析，
/// View 不再依赖父 VM 上的强类型子 VM 属性，避免"VM-in-VM"反模式。
/// </para>
/// </summary>
public sealed partial class EventBindingWorkbenchView : MviAvaloniaView<EventBindingWorkbenchViewModel>
{
    private readonly ContentControl _searchSlot;
    private readonly ContentControl _selectionSlot;
    private readonly ContentControl _detailSlot;

    /// <summary>
    /// 初始化事件绑定复杂组合根视图。
    /// </summary>
    public EventBindingWorkbenchView()
    {
        AvaloniaXamlLoader.Load(this);
        _searchSlot = this.FindControl<ContentControl>("SearchSlot")
            ?? throw new InvalidOperationException("无法找到 SearchSlot。");
        _selectionSlot = this.FindControl<ContentControl>("SelectionSlot")
            ?? throw new InvalidOperationException("无法找到 SelectionSlot。");
        _detailSlot = this.FindControl<ContentControl>("DetailSlot")
            ?? throw new InvalidOperationException("无法找到 DetailSlot。");
    }

    /// <summary>
    /// 绑定组合根 ViewModel。
    /// </summary>
    /// <param name="viewModel">组合根 ViewModel。</param>
    public new void Bind(EventBindingWorkbenchViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        EventBindingSearchPanelView searchView = new();
        EventBindingSelectionPanelView selectionView = new();
        EventBindingDetailPanelView detailView = new();
        searchView.Bind((EventBindingSearchViewModel)viewModel.CreateSearchViewModel());
        selectionView.Bind((EventBindingSelectionViewModel)viewModel.CreateSelectionViewModel());
        detailView.Bind((EventBindingDetailViewModel)viewModel.CreateDetailViewModel());
        _searchSlot.Content = searchView;
        _selectionSlot.Content = selectionView;
        _detailSlot.Content = detailView;
    }
}
