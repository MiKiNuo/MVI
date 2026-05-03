namespace MiKiNuo.Mvi.Presentation.ViewRegistry;

/// <summary>
/// 表示平台无关的 ViewModel 到 View 的注册表。
/// </summary>
public interface IMviViewRegistry
{
    /// <summary>
    /// 创建平台视图对象。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    /// <returns>平台视图对象。</returns>
    public object CreateView(object viewModel);
}
