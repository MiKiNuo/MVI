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

    internal abstract void OwnScope(IAsyncDisposable scope);
    internal abstract void OwnConstructionScope(IAsyncDisposable scope, Task constructionExited);
    internal abstract object ExecutionOwner { get; }
    internal abstract (CloseResult Result, Action Finish) CommitClose(Task<Exception?> childrenReleased);
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
        || FeatureOwnership.DependsOnExecution(owner, OperationExecutionContext.Current.Value);

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
    internal static AsyncLocal<object?> Current { get; } = new();
}
