using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Presentation.Slot;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面视图。
/// <para>
/// 4 个业务域（Inpatient / Lab / Pharmacy / Quality）共用同一套 2×2 「数据流节点」布局；
/// 视图本身不再按 PageLayout 切换 4 套子布局，仅需把 4 个数据流节点卡片渲染到 Node1..Node4 槽位。
/// </para>
/// <para>
/// 4 个节点卡片的具体 ViewModel 由 <see cref="BusinessCompositePageViewModel"/> 上的
/// <c>CreateNode1ViewModel</c>..<c>CreateNode4ViewModel</c> 工厂方法按需解析，
/// 再由 <c>[MviSlot]</c> 源生成器自动 emit <c>OnBindSlots</c> override 完成槽位绑定。
/// </para>
/// </summary>
public sealed partial class BusinessCompositePageView : MviAvaloniaView<BusinessCompositePageViewModel>
{
    /// <summary>
    /// 1 号数据流节点槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(BusinessCompositePageViewModel.CreateNode1ViewModel))]
    private MviSlotHost? _node1Slot;

    /// <summary>
    /// 2 号数据流节点槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(BusinessCompositePageViewModel.CreateNode2ViewModel))]
    private MviSlotHost? _node2Slot;

    /// <summary>
    /// 3 号数据流节点槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(BusinessCompositePageViewModel.CreateNode3ViewModel))]
    private MviSlotHost? _node3Slot;

    /// <summary>
    /// 4 号数据流节点槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(CardView), factory: nameof(BusinessCompositePageViewModel.CreateNode4ViewModel))]
    private MviSlotHost? _node4Slot;

    /// <summary>
    /// 初始化生产业务组合页面视图。
    /// </summary>
    public BusinessCompositePageView()
    {
        AvaloniaXamlLoader.Load(this);
        _node1Slot = FindRequired<MviSlotHost>("Node1Slot");
        _node2Slot = FindRequired<MviSlotHost>("Node2Slot");
        _node3Slot = FindRequired<MviSlotHost>("Node3Slot");
        _node4Slot = FindRequired<MviSlotHost>("Node4Slot");
    }

    private TControl FindRequired<TControl>(string name)
        where TControl : Control
    {
        return this.FindControl<TControl>(name)
            ?? throw new InvalidOperationException($"无法找到名为 {name} 的控件。");
    }
}
