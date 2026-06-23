using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根 ViewModel。
/// <para>
/// 3 个子组件 ViewModel（Search / Selection / Detail）由 <see cref="IEventBindingPanelFactory"/>
/// 工厂在构造期间静态注入并缓存；本 VM 仅持工厂引用，<b>不直接持有任何子 VM 引用</b>
/// （避免"VM-in-VM"反模式）。View 端通过 <see cref="CreateSearchViewModel"/>、
/// <see cref="CreateSelectionViewModel"/>、<see cref="CreateDetailViewModel"/> 三个工厂方法
/// 按需解析子 VM，再交由 ViewRegistry 创建对应 View。
/// </para>
/// </summary>
public sealed partial class EventBindingWorkbenchViewModel
    : MviViewModelBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    private readonly IEventBindingPanelFactory _panelFactory;

    /// <summary>
    /// 初始化事件绑定组合根 ViewModel。
    /// </summary>
    /// <param name="store">组合根状态存储。</param>
    /// <param name="panelFactory">3 个子组件 ViewModel 的工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选）。</param>
    public EventBindingWorkbenchViewModel(
        IMviStore<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect> store,
        IEventBindingPanelFactory panelFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(panelFactory);
        _panelFactory = panelFactory;
    }

    /// <summary>
    /// 解析搜索面板子组件 ViewModel。
    /// </summary>
    /// <returns>搜索 <c>EventBindingSearchViewModel</c> 实例。</returns>
    public object CreateSearchViewModel() => _panelFactory.CreateSearchViewModel();

    /// <summary>
    /// 解析选择面板子组件 ViewModel。
    /// </summary>
    /// <returns>选择 <c>EventBindingSelectionViewModel</c> 实例。</returns>
    public object CreateSelectionViewModel() => _panelFactory.CreateSelectionViewModel();

    /// <summary>
    /// 解析详情面板子组件 ViewModel。
    /// </summary>
    /// <returns>详情 <c>EventBindingDetailViewModel</c> 实例。</returns>
    public object CreateDetailViewModel() => _panelFactory.CreateDetailViewModel();

    /// <summary>
    /// 获取最后一次交互文本。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.LastInteractionText))]
    public partial string LastInteractionText { get; private set; }

    /// <summary>
    /// 获取交互次数。
    /// </summary>
    [MviBind(nameof(EventBindingWorkbenchState.InteractionCount))]
    public partial int InteractionCount { get; private set; }
}
