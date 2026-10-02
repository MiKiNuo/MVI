namespace MiKiNuo.Mvi;

/// <summary>为异构功能实例提供共同的生命周期入口。</summary>
public abstract class Feature
{
    private FeatureOwnership? children;
    internal FeatureMember? Membership { get; set; }

    internal static System.Runtime.CompilerServices.ConditionalWeakTable<object, Feature> ExecutionFeatures { get; } = new();

    /// <summary>获取区别于业务对象标识的唯一实例身份。</summary>
    public Guid InstanceId { get; } = Guid.NewGuid();

    /// <summary>获取该实例显式建立的子实例所有权范围。</summary>
    public FeatureOwnership Children
    {
        get
        {
            if (FeatureOwnership.IsReducing) throw new InvalidOperationException("纯状态转换不能读取实例所有权结构。");
            lock (FeatureOwnership.Gate) return children ??= new FeatureOwnership(this);
        }
    }
    /// <summary>获取实例是否已停止接纳业务及状态反馈。</summary>
    public abstract bool IsClosed { get; }

    /// <summary>提交逻辑关闭并请求在途执行取消，不等待自身执行或资源释放。</summary>
    /// <returns>已提交的关闭结果；重复关闭返回相同结果与释放票据。</returns>
    public CloseResult Close()
    {
        if (FeatureOwnership.IsReducing) throw new InvalidOperationException("纯状态转换不能改变实例生命周期或所有权。");
        return FeatureOwnership.Close(this);
    }

    /// <summary>准备整个活动实例树的业务确认，在条件仍有效时提交整体关闭。</summary>
    /// <param name="cancellationToken">只取消提交前等待及确认协作执行，不授权提前释放其资源。</param>
    /// <returns>明确的拒绝、取消、故障或原唯一关闭票据。</returns>
    public Task<CloseRequestResult> RequestCloseAsync(CancellationToken cancellationToken = default)
        => FeatureOwnership.RequestCloseAsync(this, cancellationToken);

    internal abstract void OwnScope(IAsyncDisposable scope);
    internal abstract void OwnConstructionScope(IAsyncDisposable scope, Task constructionExited);
    internal abstract object ExecutionOwner { get; }
    internal abstract (CloseResult Result, Action Finish) CommitClose(Task<Exception?> childrenReleased);
    internal abstract object ModelGate { get; }
    internal abstract (long Version, object State) ReadCloseCondition();
    internal abstract Task<OperationResult<bool>> ConfirmCondition(object state, CancellationToken cancellationToken);
}

/// <summary>表示实例已逻辑关闭，资源是否释放由独立票据表达。</summary>
public sealed class CloseResult
{
    internal CloseResult(CloseTicket ticket) => Ticket = ticket;

    /// <summary>获取本实例唯一的资源释放票据。</summary>
    public CloseTicket Ticket { get; }
}

/// <summary>保留真实释放完成信号；取消或超时等待不会改变资源归属。</summary>
public sealed class CloseTicket
{
    private readonly object owner;
    private readonly TaskCompletionSource<ReleaseResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal CloseTicket(object owner) => this.owner = owner;

    internal Task<ReleaseResult> Completion => completion.Task;
    internal bool DependsOnCurrentExecution => FeatureFactory.DependsOnConstruction(owner)
        || FeatureOwnership.DependsOnExecution(owner, OperationExecutionContext.Current.Value?.Owner)
        || OperationExecutionContext.DependsOnSynchronousCallback(owner)
        || OperationExecutionContext.DependsOnRequest(owner);

    /// <summary>获取真实释放完成及释放异常；所属操作直接访问自身票据时拒绝自等待。</summary>
    public Task<ReleaseResult> Released
    {
        get
        {
            if (DependsOnCurrentExecution)
            {
                throw new InvalidOperationException("功能操作不能等待依赖自身退出的资源释放。");
            }

            return completion.Task;
        }
    }

    internal void Complete(Exception? failure) => completion.SetResult(new ReleaseResult(failure));
}

/// <summary>表示创建失败但回收依赖调用者退出，清理继续由真实目标的释放票据负责。</summary>
public sealed class FeatureCreationException : InvalidOperationException
{
    internal FeatureCreationException(Exception failure, CloseTicket cleanup)
        : base("功能创建失败；资源清理将在所属执行真实退出后完成。", failure) => Cleanup = cleanup;

    /// <summary>获取持续跟踪本次范围安全回收及释放异常的唯一票据。</summary>
    public CloseTicket Cleanup { get; }
}

/// <summary>保存实例真实资源释放的结论。</summary>
public sealed class ReleaseResult
{
    internal ReleaseResult(Exception? exception) => Exception = exception;

    /// <summary>获取所有所属执行退出后资源是否成功释放。</summary>
    public bool Succeeded => Exception is null;

    /// <summary>获取清理或资源释放失败的异常。</summary>
    public Exception? Exception { get; }
}

/// <summary>表示业务输入或反馈在实例逻辑关闭后被拒绝。</summary>
public sealed class FeatureClosedException : InvalidOperationException
{
    /// <summary>创建实例已关闭的业务入口异常。</summary>
    public FeatureClosedException() : base("功能实例已逻辑关闭，不能接纳业务输入或状态反馈。")
    {
    }
}

internal static class OperationExecutionContext
{
    [ThreadStatic]
    private static FreshScope? synchronous;
    internal static AsyncLocal<ExecutionFrame?> Current { get; } = new();
    internal static AsyncLocal<RequestEdge?> Requests { get; } = new();

    internal static bool DependsOnRequest(object owner)
    {
        for (RequestEdge? edge = Requests.Value; edge is not null && edge.IsActive; edge = edge.Parent)
        {
            if (edge.IsActive && FeatureOwnership.DependsOnExecution(owner, edge.Caller)) return true;
        }

        return false;
    }

    internal static FreshScope Fresh() => new();

    internal static object? ResourceCaller => Current.Value?.Owner ?? synchronous?.ResourceOwner;
    internal static RequestEdge? ResourceRequests => Requests.Value ?? synchronous?.ResourceEdges;
    internal static FreshScope? ResourceLifetime => Current.Value is null ? synchronous : null;

    internal static bool DependsOnSynchronousCallback(object owner)
    {
        for (FreshScope? scope = synchronous; scope is not null; scope = scope.previous)
        {
            if (scope.ResourceOwner is not null && FeatureOwnership.DependsOnExecution(owner, scope.ResourceOwner)) return true;
            for (RequestEdge? edge = scope.ResourceEdges; edge is not null && edge.IsActive; edge = edge.Parent)
            {
                if (FeatureOwnership.DependsOnExecution(owner, edge.Caller)) return true;
            }
        }
        return false;
    }

    internal sealed class FreshScope : IDisposable
    {
        private int active = 1;
        internal bool IsActive => Volatile.Read(ref active) != 0;
        internal readonly FreshScope? previous = synchronous;
        private readonly ExecutionFrame? frame = Current.Value;
        private readonly RequestEdge? requests = Requests.Value;
        internal object? ResourceOwner => frame?.Owner ?? previous?.ResourceOwner;
        internal RequestEdge? ResourceEdges => requests ?? previous?.ResourceEdges;
        internal FreshScope() { synchronous = this; Current.Value = null; Requests.Value = null; }
        public void Dispose()
        {
            Interlocked.Exchange(ref active, 0);
            Current.Value = frame; Requests.Value = requests; synchronous = previous;
        }
    }
}

internal sealed class ExecutionFrame(object owner, Action validate)
{
    internal object Owner { get; } = owner;
    internal void Validate() => validate();
}

internal sealed class RequestEdge(object caller, object target, RequestEdge? parent, OperationExecutionContext.FreshScope? lifetime = null)
{
    private int active = 1;
    internal object Caller { get; } = caller;
    internal object Target { get; } = target;
    internal RequestEdge? Parent { get; } = parent;
    internal bool IsActive => Volatile.Read(ref active) != 0 && lifetime?.IsActive != false;
    internal void End() => Interlocked.Exchange(ref active, 0);
}
