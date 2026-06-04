using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面视图。
/// <para>
/// 4 个业务域（Inpatient / Lab / Pharmacy / Quality）共用同一套 2×2 「数据流节点」布局；
/// 视图本身不再按 PageLayout 切换 4 套子布局，仅需把 4 个数据流节点卡片渲染到 Node1..Node4 槽位。
/// </para>
/// </summary>
public sealed partial class BusinessCompositePageView : MviAvaloniaView<BusinessCompositePageViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly MviSlotHost _node1Slot;
    private readonly MviSlotHost _node2Slot;
    private readonly MviSlotHost _node3Slot;
    private readonly MviSlotHost _node4Slot;

    /// <summary>
    /// 初始化生产业务组合页面视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public BusinessCompositePageView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
        _node1Slot = FindRequired<MviSlotHost>("Node1Slot");
        _node2Slot = FindRequired<MviSlotHost>("Node2Slot");
        _node3Slot = FindRequired<MviSlotHost>("Node3Slot");
        _node4Slot = FindRequired<MviSlotHost>("Node4Slot");
    }

    /// <inheritdoc />
    public new void Bind(BusinessCompositePageViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        _node1Slot.Content = _viewRegistry.CreateView(viewModel.Node1);
        _node2Slot.Content = _viewRegistry.CreateView(viewModel.Node2);
        _node3Slot.Content = _viewRegistry.CreateView(viewModel.Node3);
        _node4Slot.Content = _viewRegistry.CreateView(viewModel.Node4);
    }

    private TControl FindRequired<TControl>(string name)
        where TControl : Control
    {
        return this.FindControl<TControl>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
