using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Runtime.DI;
namespace MiKiNuo.Mvi.Platforms.Avalonia.Views;
/// <summary>共享的非继承式 View 绑定控制器；解绑失败也不会留下模型与解析器混合状态。</summary>
/// <typeparam name="TViewModel">借用的 ViewModel。</typeparam>
public sealed class AvaloniaViewBinding<TViewModel> : IDisposable where TViewModel : class
{
    private readonly Control _view;
    private readonly Action<TViewModel, MviDisposableBag, IMviResolver> _install;
    private TViewModel? _model;
    private IMviResolver? _resolver;
    private MviDisposableBag? _bindings;
    private bool _changing;
    private bool _disposed;
    /// <summary>创建控制器，平台事件只安装一次。</summary>
    /// <param name="view">现有控件，不被本控制器拥有。</param>
    /// <param name="install">安装本代事件与槽位订阅。</param>
    public AvaloniaViewBinding(Control view, Action<TViewModel, MviDisposableBag, IMviResolver> install)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _install = install ?? throw new ArgumentNullException(nameof(install));
        _view.AttachedToVisualTree += Attached;
        _view.DetachedFromVisualTree += Detached;
        _view.DataContextChanged += DataContextChanged;
    }
    /// <summary>获取本次借用的绑定模型。</summary>
    public TViewModel? ViewModel => _model;
    /// <summary>换绑时先解除旧代，再原子安装新代；失败时进入明确的未绑定状态。</summary>
    /// <param name="model">借用的模型。</param>
    /// <param name="resolver">借用的解析器。</param>
    public void Bind(TViewModel model, IMviResolver resolver)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(model); ArgumentNullException.ThrowIfNull(resolver);
        Unbind();
        _model = model; _resolver = resolver; SetDataContext(model);
        if (_view.IsAttachedToVisualTree()) Install();
    }
    private void Install()
    {
        if (_bindings is not null || _model is null || _resolver is null) return;
        var pending = new MviDisposableBag();
        try { _install(_model, pending, _resolver); _bindings = pending; }
        catch (Exception failure)
        {
            _model = null; _resolver = null; SetDataContext(null);
            try { pending.Dispose(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
            throw;
        }
    }
    /// <summary>明确解绑，即使清理抛出异常也先清空借用关系；不释放业务对象。</summary>
    public void Unbind()
    {
        MviDisposableBag? previous = _bindings; _bindings = null;
        _model = null; _resolver = null; SetDataContext(null);
        previous?.Dispose();
    }
    private void SetDataContext(object? value)
    { _changing = true; try { _view.DataContext = value; } finally { _changing = false; } }
    private void Attached(object? sender, VisualTreeAttachmentEventArgs args) { if (!_disposed) Install(); }
    private void Detached(object? sender, VisualTreeAttachmentEventArgs args)
    { MviDisposableBag? previous = _bindings; _bindings = null; previous?.Dispose(); }
    private void DataContextChanged(object? sender, EventArgs args)
    {
        if (_changing || _disposed || ReferenceEquals(_view.DataContext, _model)) return;
        if (_view.DataContext is TViewModel model && _resolver is not null) Bind(model, _resolver);
        else Unbind();
    }
    /// <summary>永久移除平台订阅；资源仅属于 View，不包含 Store。</summary>
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _view.AttachedToVisualTree -= Attached;
        _view.DetachedFromVisualTree -= Detached;
        _view.DataContextChanged -= DataContextChanged;
        try { Unbind(); } finally { GC.SuppressFinalize(this); }
    }
}
