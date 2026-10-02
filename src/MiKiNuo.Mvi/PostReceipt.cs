namespace MiKiNuo.Mvi;

/// <summary>表示定向消息进入目标实例收件箱的接纳结论。</summary>
public enum PostResultKind
{
    /// <summary>消息已进入目标边界，不表示验证通过或处理完成。</summary>
    Accepted,
    /// <summary>目标实例的未终结投递数量已经达到容量。</summary>
    InboxFull,
    /// <summary>端口未在范围接线、已停用或所属实例已经关闭。</summary>
    TargetUnavailable,
}

/// <summary>保存一次投递的关联身份、接纳结果及后续处理结果。</summary>
/// <typeparam name="TResult">目标业务处理的返回值类型。</typeparam>
public sealed class PostReceipt<TResult>
{
    internal PostReceipt(Guid id, PostResultKind kind, Task<OperationResult<TResult>>? completion = null)
    {
        Id = id;
        Kind = kind;
        Completion = completion;
    }

    /// <summary>获取本次投递的唯一关联身份，包括未接纳的尝试。</summary>
    public Guid Id { get; }

    /// <summary>获取目标收件箱的接纳结论，与后续验证和执行结果分别表达。</summary>
    public PostResultKind Kind { get; }

    /// <summary>获取已接纳消息的原始处理结果任务；拒绝接纳时为空，等待取消不撤销投递。</summary>
    public Task<OperationResult<TResult>>? Completion { get; }
}
