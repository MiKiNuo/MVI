namespace MiKiNuo.Mvi;

/// <summary>提供一次业务操作的开始输入和强类型状态反馈入口。</summary>
/// <typeparam name="TState">功能实例的不可变业务状态类型。</typeparam>
public sealed class Operation<TState> where TState : notnull
{
    private readonly FeatureStore<TState> store;

    internal Operation(FeatureStore<TState> store, string name, Guid id, TState snapshot, CancellationToken cancellationToken,
        CancellationTokenSource? cancellationSource)
    {
        this.store = store;
        Name = name;
        Id = id;
        Snapshot = snapshot;
        CancellationToken = cancellationToken;
        CancellationSource = cancellationSource;
    }

    internal string Name { get; }

    internal Guid Id { get; }

    internal Exception? Failure { get; set; }

    internal bool Accepting { get; set; } = true;

    internal bool CancellationFinished { get; set; }
    internal bool IsConfirmation { get; set; }

    internal List<Task> Work { get; } = [];

    internal CancellationTokenSource? CancellationSource { get; }

    /// <summary>获取通过启动验证时采样的业务输入。</summary>
    public TState Snapshot { get; }

    /// <summary>获取本次执行的协作取消令牌。</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>将已启动的子工作纳入真实退出屏障，子工作可以继续登记嵌套工作。</summary>
    /// <param name="task">仍可能使用本次实例资源的工作；取消或故障期间也必须保留其归属。</param>
    public void Track(Task task)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(task);
            store.Track(this, task);
        }
        catch (Exception exception)
        {
            store.RecordFailure(this, exception);
            throw;
        }
    }

    /// <summary>将载荷组成反馈输入，依据提交时的当前状态执行纯转换。</summary>
    /// <typeparam name="TPayload">不可变业务反馈载荷的类型。</typeparam>
    /// <param name="reducer">依据当前状态和载荷计算下一状态的纯转换。</param>
    /// <param name="payload">本次反馈载荷。</param>
    /// <returns>提交完成或有效无变化时成功完成的任务；被取代时以 OperationSupersededException 明确失败。</returns>
    public ValueTask UpdateAsync<TPayload>(Func<TState, TPayload, TState> reducer, TPayload payload)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(reducer);
            store.Dispatch(new FeedbackIntent<TState, TPayload>(this, new InputIntent<TState, TPayload>(payload, reducer)));
            return ValueTask.CompletedTask;
        }
        catch (Exception exception)
        {
            store.RecordFailure(this, exception);
            return ValueTask.FromException(exception);
        }
    }
}

internal readonly struct FeedbackIntent<TState, TPayload>(Operation<TState> operation, InputIntent<TState, TPayload> transition)
    where TState : notnull
{
    internal Operation<TState> Operation { get; } = operation;

    internal TState Reduce(TState state) => transition.Reduce(state);
}
