using Avalonia;
using Avalonia.Controls;
using MiKiNuo.Mvi.Presentation.Disposables;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;

/// <summary>
/// 表示 Avalonia 平台 MVI 视图基类。
/// 内部使用 <see cref="MviDisposableBag"/> 收集 View 绑定生命周期内的可释放资源，
/// 与 Godot <c>GodotMviControlView</c> 共用同一套释放语义（重入、Dispose-after-Add 竞态等）。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public abstract class MviAvaloniaView<TViewModel> : UserControl
    where TViewModel : class
{
    private MviDisposableBag? _bindings;

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

        ClearBindings();
        DataContext = viewModel;
    }

    /// <summary>
    /// 注册随 View 重新绑定或脱离可视树自动释放的绑定资源。
    /// </summary>
    /// <param name="binding">绑定资源。</param>
    protected void RegisterBinding(IDisposable binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureBindings().Add(binding);
    }

    /// <summary>
    /// 注册随 View 重新绑定或脱离可视树自动执行的解绑动作。
    /// </summary>
    /// <param name="dispose">解绑动作。</param>
    protected void RegisterBinding(Action dispose)
    {
        ArgumentNullException.ThrowIfNull(dispose);
        EnsureBindings().Add(dispose);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearBindings();
        base.OnDetachedFromVisualTree(e);
    }

    private MviDisposableBag EnsureBindings()
    {
        return _bindings ??= new MviDisposableBag();
    }

    private void ClearBindings()
    {
        _bindings?.Dispose();
        _bindings = null;
    }
}
