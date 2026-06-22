namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 <see cref="IEventBindingPanelFactory"/> 的标准实现。
/// <para>
/// 构造时由组合根（<see cref="EventBindingWorkbenchComposition"/>）注入 3 个子 VM 并一次性缓存。
/// </para>
/// </summary>
public sealed class EventBindingPanelFactory : IEventBindingPanelFactory
{
    private readonly EventBindingSearchViewModel _searchViewModel;
    private readonly EventBindingSelectionViewModel _selectionViewModel;
    private readonly EventBindingDetailViewModel _detailViewModel;

    /// <summary>
    /// 初始化 Godot 事件绑定子组件 ViewModel 工厂。
    /// </summary>
    /// <param name="searchViewModel">搜索面板子组件 ViewModel。</param>
    /// <param name="selectionViewModel">选择面板子组件 ViewModel。</param>
    /// <param name="detailViewModel">详情面板子组件 ViewModel。</param>
    public EventBindingPanelFactory(
        EventBindingSearchViewModel searchViewModel,
        EventBindingSelectionViewModel selectionViewModel,
        EventBindingDetailViewModel detailViewModel)
    {
        ArgumentNullException.ThrowIfNull(searchViewModel);
        ArgumentNullException.ThrowIfNull(selectionViewModel);
        ArgumentNullException.ThrowIfNull(detailViewModel);

        _searchViewModel = searchViewModel;
        _selectionViewModel = selectionViewModel;
        _detailViewModel = detailViewModel;
    }

    /// <summary>
    /// 解析搜索面板子组件 ViewModel。
    /// </summary>
    /// <returns>搜索面板 ViewModel 实例。</returns>
    public object CreateSearchViewModel() => _searchViewModel;

    /// <summary>
    /// 解析选择面板子组件 ViewModel。
    /// </summary>
    /// <returns>选择面板 ViewModel 实例。</returns>
    public object CreateSelectionViewModel() => _selectionViewModel;

    /// <summary>
    /// 解析详情面板子组件 ViewModel。
    /// </summary>
    /// <returns>详情面板 ViewModel 实例。</returns>
    public object CreateDetailViewModel() => _detailViewModel;
}
