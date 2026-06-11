namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 事件绑定组合示例 3 个子面板 ViewModel（Search / Selection / Detail）的工厂。
/// <para>
/// 父 <see cref="EventBindingWorkbenchViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="EventBindingWorkbenchViewModel.CreateSearchViewModel"/>、
/// <see cref="EventBindingWorkbenchViewModel.CreateSelectionViewModel"/>、
/// <see cref="EventBindingWorkbenchViewModel.CreateDetailViewModel"/> 等方法按需解析。
/// </para>
/// </summary>
public interface IEventBindingPanelFactory
{
    /// <summary>
    /// 解析搜索面板子组件 ViewModel。
    /// </summary>
    /// <returns>搜索 <see cref="EventBindingSearchViewModel"/> 实例（缓存）。</returns>
    public object CreateSearchViewModel();

    /// <summary>
    /// 解析选择面板子组件 ViewModel。
    /// </summary>
    /// <returns>选择 <see cref="EventBindingSelectionViewModel"/> 实例（缓存）。</returns>
    public object CreateSelectionViewModel();

    /// <summary>
    /// 解析详情面板子组件 ViewModel。
    /// </summary>
    /// <returns>详情 <see cref="EventBindingDetailViewModel"/> 实例（缓存）。</returns>
    public object CreateDetailViewModel();
}
