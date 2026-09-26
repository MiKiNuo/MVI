namespace MiKiNuo.Mvi.Application.MVI.Composition;

/// <summary>拥有一个完整 Feature 的资源与生命周期。</summary>
public abstract class MviFeatureInstance : IAsyncDisposable
{
    /// <summary>仅保护实例树结构，不在锁中等待业务回调。</summary>
    private static readonly object OwnershipGate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<object> _resources;
    private readonly List<Action> _stops;
    private Task? _disposeTask;
    private MviFeatureInstance? _owner;

    /// <summary>创建实例资源所有者。</summary>
    /// <param name="id">实例身份。</param>
    /// <param name="resources">按创建顺序排列的独占资源。</param>
    /// <param name="stopActions">释放资源前须执行的停止动作（如 Store 停止准入），按声明顺序调用。</param>
    protected MviFeatureInstance(Guid id, IReadOnlyList<object> resources, IReadOnlyList<Action>? stopActions = null)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (id == Guid.Empty) throw new ArgumentException("实例标识不能为空。", nameof(id));
        if (resources.Any(resource => resource is null || resource is MviFeatureInstance))
            throw new ArgumentException("子实例必须通过 Own 接管，资源不能为 null。", nameof(resources));
        Id = id;
        Lifetime = _lifetime.Token;
        _resources = resources.Distinct(ReferenceEqualityComparer.Instance).ToList();
        _stops = stopActions?.ToList() ?? [];
    }

    /// <summary>获取实例身份。</summary>
    public Guid Id { get; }

    /// <summary>获取实例的取消信号。</summary>
    public CancellationToken Lifetime { get; }

    /// <summary>获取实例是否已开始关闭。</summary>
    public bool IsClosed { get { lock (OwnershipGate) return _disposeTask is not null; } }

    /// <summary>接管新资源或子实例；关闭后拒收并等待孤儿释放。</summary>
    /// <param name="resource">所有权转交给当前实例的资源。</param>
    /// <param name="stopAction">随所有权一并声明的停止动作（如 Store 停止准入）；无则为 null。</param>
    /// <returns>是否成功接管。</returns>
    public async ValueTask<bool> Own(object resource, Action? stopAction = null)
    {
        ArgumentNullException.ThrowIfNull(resource);
        lock (OwnershipGate)
        {
            if (_resources.Any(existing => ReferenceEquals(existing, resource)))
                throw new InvalidOperationException("资源已经由当前实例拥有。");
            if (resource is MviFeatureInstance child)
            {
                if (child._owner is not null || child.IsClosed)
                    throw new InvalidOperationException("子实例已经被拥有或正在关闭。");
                for (MviFeatureInstance? ancestor = this; ancestor is not null; ancestor = ancestor._owner)
                    if (ReferenceEquals(ancestor, child)) throw new InvalidOperationException("实例树不能形成环。");
            }
            if (!IsClosed)
            {
                if (resource is MviFeatureInstance owned) owned._owner = this;
                _resources.Add(resource);
                if (stopAction is not null) _stops.Add(stopAction);
                return true;
            }
        }

        await ReleaseResourceAsync(resource).ConfigureAwait(false);
        return false;
    }

    /// <summary>取消生命周期并逆序释放所有资源；重复调用等待同一次释放。</summary>
    /// <returns>释放完成的等待任务。</returns>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        lock (OwnershipGate)
        {
            StartTreeDisposal();
            return new ValueTask(_disposeTask!);
        }
    }

    /// <summary>在结构锁内取得稳定的父先子后快照。</summary>
    /// <param name="node">当前实例。</param>
    /// <param name="tree">结果快照。</param>
    private static void CollectTree(MviFeatureInstance node, List<MviFeatureInstance> tree)
    {
        tree.Add(node);
        foreach (MviFeatureInstance child in node._resources.OfType<MviFeatureInstance>()) CollectTree(child, tree);
    }

    /// <summary>在结构锁内同时关闭整树准入，资源按子先父后等待释放。</summary>
    private void StartTreeDisposal()
    {
        List<MviFeatureInstance> tree = [];
        CollectTree(this, tree);
        for (int index = tree.Count - 1; index >= 0; index--)
        {
            MviFeatureInstance node = tree[index];
            node._disposeTask ??= Task.Run(node.ReleaseAsync);
        }
    }

    private async Task ReleaseAsync()
    {
        List<Exception> failures = [];
        foreach (Action stop in _stops)
        {
            try { stop(); }
            catch (Exception exception) { failures.Add(exception); }
        }
        try { await _lifetime.CancelAsync().ConfigureAwait(false); }
        catch (Exception exception) { failures.Add(exception); }
        foreach (MviFeatureInstance child in _resources.OfType<MviFeatureInstance>().Reverse())
        {
            Task closing;
            lock (OwnershipGate) closing = child._disposeTask!;
            try { await closing.ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }
        for (int index = _resources.Count - 1; index >= 0; index--)
        {
            if (_resources[index] is MviFeatureInstance) continue;
            try { await ReleaseResourceAsync(_resources[index]).ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }

        _lifetime.Dispose();
        if (failures.Count != 0) throw new AggregateException("实例资源释放失败。", failures);
    }

    private static async ValueTask ReleaseResourceAsync(object resource)
    {
        if (resource is IAsyncDisposable asynchronous)
        {
            await asynchronous.DisposeAsync().ConfigureAwait(false);
        }
        else if (resource is IDisposable synchronous)
        {
            synchronous.Dispose();
        }
    }
}

/// <summary>提供强类型视图模型的 Feature 实例。</summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public sealed class MviFeatureInstance<TViewModel> : MviFeatureInstance
{
    /// <summary>建立视图模型与按创建顺序排列的独占资源的所有权。</summary>
    /// <param name="id">实例身份。</param>
    /// <param name="vm">实例视图模型。</param>
    /// <param name="resources">按创建顺序排列的独占资源，包含需要释放的视图模型。</param>
    /// <param name="stopActions">释放资源前须执行的停止动作（如 Store 停止准入），按声明顺序调用。</param>
    public MviFeatureInstance(Guid id, TViewModel vm, IReadOnlyList<object> resources, IReadOnlyList<Action>? stopActions = null)
        : base(id, resources, stopActions)
    {
        ViewModel = vm;
    }

    /// <summary>获取实例视图模型。</summary>
    public TViewModel ViewModel { get; }
}
