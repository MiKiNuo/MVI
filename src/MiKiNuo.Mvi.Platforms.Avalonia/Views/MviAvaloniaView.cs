using Avalonia;
using Avalonia.Controls;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;

/// <summary>
/// 表示 Avalonia 平台 MVI 视图基类。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public abstract class MviAvaloniaView<TViewModel> : UserControl
    where TViewModel : class
{
    private readonly List<IDisposable> _bindings = [];

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
        _bindings.Add(binding);
    }

    /// <summary>
    /// 注册随 View 重新绑定或脱离可视树自动执行的解绑动作。
    /// </summary>
    /// <param name="dispose">解绑动作。</param>
    protected void RegisterBinding(Action dispose)
    {
        ArgumentNullException.ThrowIfNull(dispose);
        RegisterBinding(new ActionDisposable(dispose));
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearBindings();
        base.OnDetachedFromVisualTree(e);
    }

    private void ClearBindings()
    {
        for (int index = _bindings.Count - 1; index >= 0; index--)
        {
            _bindings[index].Dispose();
        }

        _bindings.Clear();
    }

    private sealed class ActionDisposable : IDisposable
    {
        private readonly Action _dispose;
        private bool _isDisposed;

        public ActionDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _dispose();
            _isDisposed = true;
        }
    }
}
