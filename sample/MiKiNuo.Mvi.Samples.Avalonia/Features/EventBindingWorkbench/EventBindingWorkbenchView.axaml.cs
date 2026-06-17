using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Presentation.Slot;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定复杂组合根视图。
/// <para>
/// 3 个子组件 ViewModel 由 <see cref="EventBindingWorkbenchViewModel"/> 工厂方法按需解析，
/// View 不再依赖父 VM 上的强类型子 VM 属性，避免"VM-in-VM"反模式。
/// </para>
/// <para>
/// 3 个槽位（SearchSlot / SelectionSlot / DetailSlot）通过 <c>[MviSlot]</c> 特性声明，
/// 由 <c>MviCompositeSlotBindingGenerator</c> 源生成器自动 emit <c>OnBindSlots</c> override。
/// </para>
/// </summary>
public sealed partial class EventBindingWorkbenchView : MviAvaloniaView<EventBindingWorkbenchViewModel>
{
    /// <summary>
    /// 搜索面板槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(EventBindingSearchPanelView), factory: nameof(EventBindingWorkbenchViewModel.CreateSearchViewModel))]
    private MviSlotHost? _searchSlot;

    /// <summary>
    /// 选择面板槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(EventBindingSelectionPanelView), factory: nameof(EventBindingWorkbenchViewModel.CreateSelectionViewModel))]
    private MviSlotHost? _selectionSlot;

    /// <summary>
    /// 详情面板槽位（由源生成器自动绑定）。
    /// </summary>
    [MviSlot(typeof(EventBindingDetailPanelView), factory: nameof(EventBindingWorkbenchViewModel.CreateDetailViewModel))]
    private MviSlotHost? _detailSlot;

    /// <summary>
    /// 初始化事件绑定复杂组合根视图。
    /// </summary>
    public EventBindingWorkbenchView()
    {
        AvaloniaXamlLoader.Load(this);
        _searchSlot = this.FindControl<MviSlotHost>("SearchSlot")
            ?? throw new InvalidOperationException("无法找到 SearchSlot。");
        _selectionSlot = this.FindControl<MviSlotHost>("SelectionSlot")
            ?? throw new InvalidOperationException("无法找到 SelectionSlot。");
        _detailSlot = this.FindControl<MviSlotHost>("DetailSlot")
            ?? throw new InvalidOperationException("无法找到 DetailSlot。");
    }
}
