namespace MiKiNuo.Mvi;

/// <summary>为异构功能实例提供共同的生命周期入口。</summary>
public abstract class Feature
{
    /// <summary>获取实例是否已停止接纳业务及状态反馈。</summary>
    public abstract bool IsClosed { get; }

    /// <summary>提交逻辑关闭并请求在途执行取消，不等待自身执行或资源释放。</summary>
    /// <returns>已提交的关闭结果；重复关闭返回相同结果与释放票据。</returns>
    public abstract CloseResult Close();

    internal abstract void OwnScope(IAsyncDisposable scope);
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

    /// <summary>获取真实释放完成及释放异常；所属操作直接访问自身票据时拒绝自等待。</summary>
    public Task<ReleaseResult> Released
    {
        get
        {
            if (ReferenceEquals(OperationExecutionContext.Current.Value, owner))
            {
                throw new InvalidOperationException("功能操作不能等待依赖自身退出的资源释放。");
            }

            return completion.Task;
        }
    }

    internal void Complete(Exception? failure) => completion.SetResult(new ReleaseResult(failure));
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
