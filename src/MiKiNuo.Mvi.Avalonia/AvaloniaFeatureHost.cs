using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace MiKiNuo.Mvi.Platforms.Avalonia;

/// <summary>只管理本地 View 连接的功能挂载宿主，实例逻辑生命周期由其所有者管理。</summary>
public sealed class AvaloniaFeatureHost : ContentControl, IDisposable
{
    private Feature? feature;
    private Func<Control>? createView;
    private Control? view;
    private bool disposed;

    /// <summary>使用 ContentControl 的原生主题模板呈现挂载内容。</summary>
    protected override Type StyleKeyOverride => typeof(ContentControl);

    /// <summary>挂载已存在的独立实例，同一实例的稳定挂载复用现有 View。</summary>
    /// <typeparam name="TFeature">宿主拥有的功能类型。</typeparam>
    /// <typeparam name="TView">拥有投影和输入连接的可释放原生 View 类型。</typeparam>
    /// <param name="instance">由逻辑所有者管理生命周期的现有实例。</param>
    /// <param name="factory">只创建本地 View，不创建或关闭业务实例的工厂。</param>
    public void Mount<TFeature, TView>(TFeature instance, Func<TFeature, TView> factory)
        where TFeature : Feature where TView : Control, IDisposable
    {
        Dispatcher.UIThread.VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(factory);
        if (ReferenceEquals(feature, instance) && view is not null && ReferenceEquals(Content, view)) return;
        // 同实例只能有一个活动投影；调用者已替换 Content 时先结束失去挂载的旧连接。
        if (ReferenceEquals(feature, instance)) ReleaseView();
        TView next = factory(instance) ?? throw new InvalidOperationException("View 工厂不能返回空值。");
        ReleaseView();
        feature = instance;
        createView = () => factory(instance);
        view = next;
        Content = next;
    }

    /// <summary>结束挂载并释放本地 View，保留功能实例及其执行。</summary>
    public void Unmount()
    {
        Dispatcher.UIThread.VerifyAccess();
        feature = null;
        createView = null;
        ReleaseView();
    }

    /// <summary>永久释放该宿主拥有的 View，不关闭或等待功能实例。</summary>
    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return;
        disposed = true;
        Unmount();
        GC.SuppressFinalize(this);
    }

    /// <summary>视觉卸载仅释放该次本地展示，保留再次连接时的实例与工厂。</summary>
    /// <param name="e">原生视觉卸载事件。</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ReleaseView();
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>视觉重新连接时从保留的实例创建最新投影。</summary>
    /// <param name="e">原生视觉连接事件。</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (feature?.IsClosed == true)
        {
            Unmount();
            return;
        }

        if (!disposed && view is null && createView is not null && Content is null)
        {
            Control next = createView();
            view = next;
            Content = next;
        }
    }

    private void ReleaseView()
    {
        Control? previous = view;
        view = null;
        if (previous is null) return;
        if (ReferenceEquals(Content, previous)) Content = null;
        ((IDisposable)previous).Dispose();
    }
}
