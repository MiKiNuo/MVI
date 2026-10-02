namespace MiKiNuo.Mvi;

/// <summary>表达整个所有权范围请求关闭的结论。</summary>
public enum CloseRequestKind
{
    /// <summary>条件仍有效，整个范围已提交逻辑关闭。</summary>
    Closed,
    /// <summary>至少一个业务确认拒绝，范围保持可用。</summary>
    Rejected,
    /// <summary>最终提交前调用方停止等待，不证明确认执行已经退出。</summary>
    WaitCanceled,
    /// <summary>业务确认发生故障，范围没有因此提交关闭。</summary>
    Faulted,
}

/// <summary>保存请求关闭结果及已提交范围的原释放票据。</summary>
public sealed class CloseRequestResult
{
    internal CloseRequestResult(CloseRequestKind kind, CloseResult? close = null, Exception? exception = null)
    { Kind = kind; Close = close; Exception = exception; }

    /// <summary>获取整个关闭请求的结论。</summary>
    public CloseRequestKind Kind { get; }
    /// <summary>获取已关闭时的原唯一结果及真实释放票据。</summary>
    public CloseResult? Close { get; }
    /// <summary>获取确认发生的原始故障。</summary>
    public Exception? Exception { get; }
}

/// <summary>提供强类型关闭条件与仍可能使用资源的确认工作归属。</summary>
/// <typeparam name="TState">本实例的不可变业务状态类型。</typeparam>
public sealed class CloseConfirmation<TState> where TState : notnull
{
    private readonly Operation<TState> operation;
    internal CloseConfirmation(Operation<TState> operation) => this.operation = operation;
    /// <summary>获取本轮准备时捕获的不可变业务条件。</summary>
    public TState Snapshot => operation.Snapshot;
    /// <summary>获取本轮确认的协作取消令牌。</summary>
    public CancellationToken CancellationToken => operation.CancellationToken;
    /// <summary>将确认子工作纳入真实退出屏障，取消等待后仍保持资源归属。</summary>
    /// <param name="task">仍可能使用本实例范围资源的子工作。</param>
    public void Track(Task task) => operation.Track(task);
}

internal sealed record CloseCondition(Feature Feature, long Revision, long StateVersion, object State);
