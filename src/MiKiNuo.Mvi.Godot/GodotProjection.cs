using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using global::Godot;

namespace MiKiNuo.Mvi.Platforms.Godot;

/// <summary>为树内 View 管理主线程投影与原生输入连接，退出树时自动断开。</summary>
public sealed class GodotProjection : IDisposable
{
    private readonly Node view;
    private readonly SceneTree tree;
    private readonly int threadId;
    private readonly object gate = new();
    private readonly ConcurrentQueue<Action> actions = new();
    private readonly List<IDisposable> connections = [];
    private bool disposed;

    /// <summary>在 Godot 主线程为已经进入场景树的 View 建立帧调度连接。</summary>
    /// <param name="view">拥有本地连接的原生 View 节点。</param>
    public GodotProjection(Node view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("必须在 Godot 主线程建立 View 连接。");
        if (!view.IsInsideTree()) throw new InvalidOperationException("View 必须已进入场景树。");
        this.view = view;
        tree = view.GetTree();
        threadId = System.Environment.CurrentManagedThreadId;
        tree.ProcessFrame += Drain;
        view.TreeExiting += Dispose;
    }

    /// <summary>创建由本连接拥有的本地投影，默认合并等待展示的快照。</summary>
    /// <typeparam name="TProjection">功能生成的投影类型。</typeparam>
    /// <param name="create">功能生成的投影工厂。</param>
    /// <param name="mode">等待快照的展示方式；EveryCommit 仍逐个展示中间快照。</param>
    /// <returns>在主线程读取和回写的强类型投影。</returns>
    public TProjection Create<TProjection>(Func<Action<Action>, ProjectionMode, TProjection> create,
        ProjectionMode mode = ProjectionMode.Coalesce) where TProjection : IDisposable
    {
        ArgumentNullException.ThrowIfNull(create);
        VerifyActive();
        TProjection projection = create(Post, mode);
        connections.Add(projection);
        return projection;
    }

    /// <summary>将原生文本输入连接到一个生成的可编辑属性，并回显已提交值。</summary>
    /// <typeparam name="TProjection">功能生成的投影类型。</typeparam>
    /// <param name="projection">本地生成投影。</param>
    /// <param name="target">所属 View 的原生文本控件。</param>
    /// <param name="input">直接选择生成的可编辑字符串属性。</param>
    /// <returns>可提前断开且由本连接统一释放的输入连接。</returns>
    public IDisposable BindInput<TProjection>(TProjection projection, LineEdit target,
        Expression<Func<TProjection, string>> input) where TProjection : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(input);
        VerifyActive();
        if (input.Body is not MemberExpression { Member: PropertyInfo property } member
            || member.Expression != input.Parameters[0] || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
        {
            throw new ArgumentException("输入必须直接选择生成投影的可编辑属性。", nameof(input));
        }

        Func<TProjection, string> read = property.GetMethod.CreateDelegate<Func<TProjection, string>>();
        Action<TProjection, string> write = property.SetMethod.CreateDelegate<Action<TProjection, string>>();
        InputConnection connection = null!;
        bool writingTarget = false;
        void UpdateTarget()
        {
            if (connection.IsDisposed) return;
            writingTarget = true;
            try { target.Text = read(projection); }
            finally { writingTarget = false; }
        }

        LineEdit.TextChangedEventHandler targetChanged = value => { if (!connection.IsDisposed && !writingTarget) write(projection, value); };
        PropertyChangedEventHandler sourceChanged = (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == property.Name) UpdateTarget();
        };
        connection = new(this, projection, target, sourceChanged, targetChanged);
        UpdateTarget();
        target.TextChanged += targetChanged;
        projection.PropertyChanged += sourceChanged;
        connections.Add(connection);
        return connection;
    }

    /// <summary>在主线程释放原生事件、输入及投影，清除排队的旧 View 回调。</summary>
    public void Dispose()
    {
        VerifyAccess();
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            actions.Clear();
        }

        tree.ProcessFrame -= Drain;
        view.TreeExiting -= Dispose;
        for (int index = connections.Count - 1; index >= 0; index--) connections[index].Dispose();
        connections.Clear();
    }

    private void Post(Action action)
    {
        lock (gate)
        {
            if (!disposed) actions.Enqueue(action);
        }
    }

    private void Drain()
    {
        VerifyAccess();
        // 仅执行入帧时已有的批次；执行过程中重新排队的展示留到下一帧。
        int remaining = actions.Count;
        while (remaining-- > 0 && actions.TryDequeue(out Action? action)) action();
    }

    private void VerifyAccess()
    {
        if (System.Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException("View 连接只能在所属 Godot 主线程访问。");
    }

    private void VerifyActive()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class InputConnection(GodotProjection owner, INotifyPropertyChanged projection, LineEdit target,
        PropertyChangedEventHandler sourceChanged, LineEdit.TextChangedEventHandler targetChanged) : IDisposable
    {
        internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            owner.VerifyAccess();
            if (IsDisposed) return;
            IsDisposed = true;
            if (GodotObject.IsInstanceValid(target)) target.TextChanged -= targetChanged;
            projection.PropertyChanged -= sourceChanged;
        }
    }
}
