using Avalonia.Controls;

namespace MiKiNuo.Mvi.Presentation.Platforms.Avalonia.ViewRegistry;

/// <summary>
/// 表示 Avalonia ViewModel 到 View 的注册表。
/// </summary>
public interface IMviViewRegistry
{
    /// <summary>
    /// 创建视图。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    /// <returns>Avalonia 控件。</returns>
    public Control CreateView(object viewModel);
}
