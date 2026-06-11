namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合示例 3 个子面板 ViewModel（Search / Selection / Detail）的工厂。
/// <para>
/// 父 <see cref="EventBindingWorkbenchViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="EventBindingWorkbenchViewModel.CreateSearchViewModel"/>、
/// <see cref="EventBindingWorkbenchViewModel.CreateSelectionViewModel"/>、
/// <see cref="EventBindingWorkbenchViewModel.CreateDetailViewModel"/> 等方法按需解析。
/// </para>
/// <para>
/// 实现内部在构造时一次性缓存 3 个子 VM，避免父 VM 长期持有可变子 VM 引用导致的
/// "VM-in-VM"反模式（State 体积膨胀、缓存无效状态、序列化失真）。
/// 3 个子组件均不参与父 Store 的状态计算，因此工厂缓存与按需解析在语义上等价。
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
