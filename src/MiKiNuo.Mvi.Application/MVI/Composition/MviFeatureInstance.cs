namespace MiKiNuo.Mvi.Application.MVI.Composition;

/// <summary>拥有一个完整 Feature 的资源与生命周期。</summary>
public abstract class MviFeatureInstance : IAsyncDisposable
{
    /// <summary>仅保护实例树结构，不在锁中等待业务回调。</summary>
    private static readonly object OwnershipGate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<object> _resources;
    private Task? _disposeTask;
    private Task? _closeTask;
    private MviFeatureInstance? _owner;
    private bool _preparing;

    /// <summary>创建实例资源所有者。</summary>
    /// <param name="id">实例身份。</param>
    /// <param name="resources">按创建顺序排列的独占资源。</param>
    protected MviFeatureInstance(Guid id, IReadOnlyList<object> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (id == Guid.Empty) throw new ArgumentException("实例标识不能为空。", nameof(id));
        if (resources.Any(resource => resource is null || resource is MviFeatureInstance))
            throw new ArgumentException("子实例必须通过 Own 接管，资源不能为 null。", nameof(resources));
        Id = id;
        Lifetime = _lifetime.Token;
        _resources = resources.Distinct(ReferenceEqualityComparer.Instance).ToList();
    }

    /// <summary>获取实例身份。</summary>
    public Guid Id { get; }

    /// <summary>获取实例的取消信号。</summary>
    public CancellationToken Lifetime { get; }

    /// <summary>获取实例是否已开始关闭。</summary>
    public bool IsClosed { get { lock (OwnershipGate) return _disposeTask is not null; } }

    /// <summary>接管新资源或子实例；关闭后拒收并等待孤儿释放。</summary>
    /// <param name="resource">所有权转交给当前实例的资源。</param>
    /// <returns>是否成功接管。</returns>
    public async ValueTask<bool> Own(object resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        lock (OwnershipGate)
        {
            if (_preparing) throw new InvalidOperationException("关闭准备期间禁止修改实例树。");
            if (_resources.Any(existing => ReferenceEquals(existing, resource)))
                throw new InvalidOperationException("资源已经由当前实例拥有。");
            if (resource is MviFeatureInstance child)
            {
                if (child._owner is not null || child._preparing || child.IsClosed)
                    throw new InvalidOperationException("子实例已经被拥有或正在关闭。");
                for (MviFeatureInstance? ancestor = this; ancestor is not null; ancestor = ancestor._owner)
                    if (ReferenceEquals(ancestor, child)) throw new InvalidOperationException("实例树不能形成环。");
            }
            if (!IsClosed)
            {
                if (resource is MviFeatureInstance owned) owned._owner = this;
                _resources.Add(resource);
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
            if (_preparing) throw new InvalidOperationException("关闭准备期间请通过 TryCloseAsync 完成关闭。");
            StartTreeDisposal();
            return new ValueTask(_disposeTask!);
        }
    }

    /// <summary>经宿主中介接线确认整个子树，失败撤回，全部同意后从叶子关闭。</summary>
    /// <param name="prepare">准备请求，返回确认的编辑版本；空值表示拒绝且未建立准备。</param>
    /// <param name="validate">在目标自身准入门中原子核验版本并冻结编辑。</param>
    /// <param name="resume">撤回准备并恢复编辑的请求。</param>
    /// <param name="commit">完成关闭准备的控制请求。</param>
    /// <param name="cancellationToken">准备阶段取消信号，提交后仍完成资源释放。</param>
    /// <returns>全部同意并完成关闭时为真。</returns>
    public async ValueTask<bool> TryCloseAsync(
        Func<Guid, CancellationToken, ValueTask<long?>> prepare,
        Func<Guid, long, CancellationToken, ValueTask<bool>> validate,
        Func<Guid, long, ValueTask> resume,
        Func<Guid, long, ValueTask> commit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepare);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(commit);
        List<MviFeatureInstance> tree = [];
        Task? existingClose;
        lock (OwnershipGate)
        {
            existingClose = _closeTask ?? _disposeTask;
            if (existingClose is null)
            {
                CollectTree(this, tree);
                // 已关闭节点仍由资源树等待回收，但不再参与业务确认。
                tree.RemoveAll(node => node.IsClosed);
                if (tree.Any(node => node._preparing))
                    throw new InvalidOperationException("实例树已有关闭操作。");
                foreach (MviFeatureInstance node in tree) node._preparing = true;
            }
        }
        if (existingClose is not null)
        {
            await existingClose.ConfigureAwait(false);
            return true;
        }

        List<(MviFeatureInstance Node, long Version)> prepared = [];
        bool approved = false;
        try
        {
            foreach (MviFeatureInstance node in tree)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long? version = await prepare(node.Id, cancellationToken).ConfigureAwait(false);
                if (version is null) return false;
                prepared.Add((node, version.Value));
            }
            foreach ((MviFeatureInstance node, long version) in prepared)
                if (!await validate(node.Id, version, cancellationToken).ConfigureAwait(false)) return false;
            cancellationToken.ThrowIfCancellationRequested();
            approved = true;
        }
        finally
        {
            if (!approved)
            {
                List<Exception> errors = [];
                foreach ((MviFeatureInstance node, long version) in prepared.AsEnumerable().Reverse())
                {
                    try { await resume(node.Id, version).ConfigureAwait(false); }
                    catch (Exception exception) { errors.Add(exception); }
                }
                lock (OwnershipGate) foreach (MviFeatureInstance node in tree) node._preparing = false;
                if (errors.Count != 0) throw new AggregateException("关闭准备撤回失败。", errors);
            }
        }

        List<Exception> failures = [];
        foreach ((MviFeatureInstance node, long version) in prepared.AsEnumerable().Reverse())
        {
            try { await commit(node.Id, version).ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }
        Task closing;
        lock (OwnershipGate)
        {
            foreach (MviFeatureInstance node in tree) node._preparing = false;
            StartTreeDisposal();
            closing = _closeTask = CompleteCloseAsync(_disposeTask!, failures);
        }
        await closing.ConfigureAwait(false);
        return true;
    }

    private static async Task CompleteCloseAsync(Task closing, List<Exception> failures)
    {
        try { await closing.ConfigureAwait(false); }
        catch (Exception exception) { failures.Add(exception); }
        if (failures.Count != 0) throw new AggregateException("关闭提交或回收失败。", failures);
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
        if (tree.Any(node => node._preparing)) throw new InvalidOperationException("子实例正在关闭准备。");
        for (int index = tree.Count - 1; index >= 0; index--)
        {
            MviFeatureInstance node = tree[index];
            node._disposeTask ??= Task.Run(node.ReleaseAsync);
        }
    }

    private async Task ReleaseAsync()
    {
        List<Exception> failures = [];
        foreach (Store.IMviStoreLifetime store in _resources.OfType<Store.IMviStoreLifetime>()) store.Stop();
        try { await _lifetime.CancelAsync().ConfigureAwait(false); }
        catch (Exception exception) { failures.Add(exception); }
        foreach (MviFeatureInstance child in _resources.OfType<MviFeatureInstance>().Reverse())
        {
            Task closing;
            lock (OwnershipGate) closing = child._closeTask ?? child._disposeTask!;
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
    public MviFeatureInstance(Guid id, TViewModel vm, IReadOnlyList<object> resources) : base(id, resources)
    {
        ViewModel = vm;
    }

    /// <summary>获取实例视图模型。</summary>
    public TViewModel ViewModel { get; }
}
