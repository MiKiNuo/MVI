using Avalonia.Controls;

namespace MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Views;

/// <summary>
/// 表示 Avalonia 平台 MVI 视图基类。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public abstract class MviAvaloniaView<TViewModel> : UserControl
    where TViewModel : class
{
    /// <summary>
    /// 获取强类型 ViewModel。
    /// </summary>
    protected TViewModel ViewModel
    {
        get
        {
            if (DataContext is TViewModel viewModel)
            {
                return viewModel;
            }

            throw new InvalidOperationException($"当前视图未绑定 ViewModel：{typeof(TViewModel).FullName}");
        }
    }

    /// <summary>
    /// 绑定 ViewModel。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    public void Bind(TViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        DataContext = viewModel;
    }
}
