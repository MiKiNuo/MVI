namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合示例 3 个子面板 ViewModel 的工厂。
/// <para>
/// 父 ViewModel 仅持此工厂（不直接持有任何子 VM 引用），
/// 由父 VM 按需解析子 VM，避免"VM-in-VM"反模式。
/// </para>
/// </summary>
public interface IEventBindingPanelFactory
{
    /// <summary>
    /// 解析搜索面板子组件 ViewModel。
    /// </summary>
    /// <returns>搜索面板 ViewModel 实例（缓存）。</returns>
    public object CreateSearchViewModel();

    /// <summary>
    /// 解析选择面板子组件 ViewModel。
    /// </summary>
    /// <returns>选择面板 ViewModel 实例（缓存）。</returns>
    public object CreateSelectionViewModel();

    /// <summary>
    /// 解析详情面板子组件 ViewModel。
    /// </summary>
    /// <returns>详情面板 ViewModel 实例（缓存）。</returns>
    public object CreateDetailViewModel();
}
