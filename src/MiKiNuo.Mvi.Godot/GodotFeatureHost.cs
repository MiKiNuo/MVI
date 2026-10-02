using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot;

/// <summary>只管理自有子 View 的本地挂载，Feature 逻辑生命周期由其所有者管理。</summary>
public sealed class GodotFeatureHost : IDisposable
{
    private readonly Node container;
    private readonly int threadId;
    private Feature? feature;
    private Func<Node>? createView;
    private Node? view;
    private long revision;
    private bool disposed;

    /// <summary>在 Godot 主线程为原生容器建立本地挂载连接。</summary>
    /// <param name="container">允许保留其他无关子节点的原生容器。</param>
    public GodotFeatureHost(Node container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("必须在 Godot 主线程建立宿主。");
        this.container = container;
        threadId = System.Environment.CurrentManagedThreadId;
        container.TreeExiting += ReleaseView;
        container.TreeEntered += ScheduleRestore;
    }

    /// <summary>将实例挂载安排到原生安全阶段，稳定挂载同一实例时复用当前 View。</summary>
    /// <remarks>挂载请求在 Godot 安全的 deferred 阶段创建 View；方法返回时工厂可能尚未执行。</remarks>
    /// <typeparam name="TFeature">由业务所有者管理的独立实例类型。</typeparam>
    /// <typeparam name="TView">退出树时释放本地连接的原生 View 类型。</typeparam>
    /// <param name="instance">已经创建的业务实例。</param>
    /// <param name="factory">只创建本地节点，不创建或关闭实例的工厂。</param>
    public void Mount<TFeature, TView>(TFeature instance, Func<TFeature, TView> factory)
        where TFeature : Feature where TView : Node
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(factory);
        if (instance.IsClosed) throw new FeatureClosedException();
        if (ReferenceEquals(feature, instance) && GodotObject.IsInstanceValid(view) && !view!.IsQueuedForDeletion() && view.GetParent() == container) return;
        ReleaseView();
        feature = instance;
        createView = () => factory(instance) ?? throw new InvalidOperationException("View 工厂不能返回空值。");
        ScheduleRestore();
    }

    /// <summary>结束当前挂载，只释放本地节点与连接，保留业务实例。</summary>
    public void Unmount()
    {
        VerifyAccess();
        feature = null;
        createView = null;
        ReleaseView();
    }

    /// <summary>永久断开容器事件并结束自有 View，不关闭业务实例。</summary>
    public void Dispose()
    {
        VerifyAccess();
        if (disposed) return;
        disposed = true;
        Unmount();
        if (GodotObject.IsInstanceValid(container))
        {
            container.TreeExiting -= ReleaseView;
            container.TreeEntered -= ScheduleRestore;
        }
    }

    private void ScheduleRestore()
    {
        long requested = ++revision;
        Callable.From(() =>
        {
            if (!disposed && revision == requested) Restore();
        }).CallDeferred();
    }

    private void Restore()
    {
        if (disposed || feature is null || createView is null || !GodotObject.IsInstanceValid(container) || !container.IsInsideTree()) return;
        if (feature.IsClosed) { Unmount(); return; }
        if (GodotObject.IsInstanceValid(view)) return;
        Node next = createView();
        if (next.GetParent() is not null) throw new InvalidOperationException("View 工厂必须返回未挂载的新节点。");
        view = next;
        container.AddChild(next);
        if (next.GetParent() != container)
        {
            view = null;
            next.QueueFree();
            throw new InvalidOperationException("原生容器未接纳 View 节点。");
        }
    }

    private void ReleaseView()
    {
        VerifyAccess();
        revision++;
        Node? previous = view;
        view = null;
        if (!GodotObject.IsInstanceValid(previous)) return;
        // 容器自身退出回调发生在其孩子退出后；仅移除宿主实际拥有的节点。
        if (previous!.GetParent() == container) container.RemoveChild(previous);
        previous.QueueFree();
    }

    private void VerifyAccess()
    {
        if (System.Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException("宿主只能在所属 Godot 主线程访问。");
    }
}
