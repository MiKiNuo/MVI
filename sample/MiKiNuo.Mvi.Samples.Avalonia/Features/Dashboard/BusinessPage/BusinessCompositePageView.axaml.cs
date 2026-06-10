using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面视图。
/// <para>
/// 4 个业务域（Inpatient / Lab / Pharmacy / Quality）共用同一套 2×2 「数据流节点」布局；
/// 视图本身不再按 PageLayout 切换 4 套子布局，仅需把 4 个数据流节点卡片渲染到 Node1..Node4 槽位。
/// </para>
/// <para>
/// 4 个节点卡片的具体 ViewModel 由 <see cref="CardStoreFactory"/> 按
/// <see cref="BusinessCompositePageViewModel.Node1Key"/>..<see cref="BusinessCompositePageViewModel.Node4Key"/>
/// 解析，本 View 不再从 ViewModel 拉取子 VM 引用——ViewModel 内部不持有任何 CardViewModel 引用。
/// </para>
/// </summary>
public sealed partial class BusinessCompositePageView : MviAvaloniaView<BusinessCompositePageViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly CardStoreFactory _cardStoreFactory;
    private readonly MviSlotHost _node1Slot;
    private readonly MviSlotHost _node2Slot;
    private readonly MviSlotHost _node3Slot;
    private readonly MviSlotHost _node4Slot;

    /// <summary>
    /// 初始化生产业务组合页面视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    /// <param name="cardStoreFactory">仪表板卡片工厂（用于按 PageKey 解析具体 CardViewModel）。</param>
    public BusinessCompositePageView(IMviViewRegistry viewRegistry, CardStoreFactory cardStoreFactory)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);
        ArgumentNullException.ThrowIfNull(cardStoreFactory);

        _viewRegistry = viewRegistry;
        _cardStoreFactory = cardStoreFactory;
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
        _node1Slot.Content = _viewRegistry.CreateView(_cardStoreFactory.GetViewModel(viewModel.Node1Key));
        _node2Slot.Content = _viewRegistry.CreateView(_cardStoreFactory.GetViewModel(viewModel.Node2Key));
        _node3Slot.Content = _viewRegistry.CreateView(_cardStoreFactory.GetViewModel(viewModel.Node3Key));
        _node4Slot.Content = _viewRegistry.CreateView(_cardStoreFactory.GetViewModel(viewModel.Node4Key));
    }

    private TControl FindRequired<TControl>(string name)
        where TControl : Control
    {
        return this.FindControl<TControl>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
