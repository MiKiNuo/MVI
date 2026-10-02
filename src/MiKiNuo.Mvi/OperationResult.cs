using System.Collections.Immutable;

namespace MiKiNuo.Mvi;

/// <summary>表示操作执行的结束种类，与业务返回值分别表达。</summary>
public enum OperationResultKind
{
    /// <summary>业务方法和所属工作已经退出，有关状态已经提交。</summary>
    Completed,
    /// <summary>操作未获准启动。</summary>
    Rejected,
    /// <summary>操作受到协作取消。</summary>
    Canceled,
    /// <summary>操作已被后续执行取代。</summary>
    Superseded,
    /// <summary>操作或纯规则发生非预期异常。</summary>
    Faulted,
}

/// <summary>保存一次操作调用的执行结果和强类型业务返回值。</summary>
/// <typeparam name="TResult">业务方法的返回值类型。</typeparam>
public sealed class OperationResult<TResult>
{
    internal OperationResult(string name, Guid operationId, OperationResultKind kind, TResult? value = default,
        string? reason = null, Exception? exception = null)
    {
        Name = name;
        OperationId = operationId;
        Kind = kind;
        Value = value;
        Reason = reason;
        Exception = exception;
    }

    /// <summary>获取声明的业务操作名称。</summary>
    public string Name { get; }

    /// <summary>获取本次调用的唯一身份。</summary>
    public Guid OperationId { get; }

    /// <summary>获取本次调用的执行结束种类。</summary>
    public OperationResultKind Kind { get; }

    /// <summary>获取业务方法正常完成时的返回值，业务失败值也属于正常完成。</summary>
    public TResult? Value { get; }

    /// <summary>获取本次结果是否包含业务方法的有效返回值。</summary>
    public bool HasValue => Kind == OperationResultKind.Completed;

    /// <summary>获取拒绝或异常的可识别原因。</summary>
    public string? Reason { get; }

    /// <summary>获取导致故障的异常。</summary>
    public Exception? Exception { get; }
}

/// <summary>保存同一快照中的操作运行身份与最近一次调用反馈。</summary>
public sealed class OperationState
{
    internal OperationState(ImmutableList<Guid> runningIds, Guid lastAttemptId, OperationResultKind? lastResult,
        string? reason = null, Exception? exception = null)
    {
        RunningIds = runningIds;
        LastAttemptId = lastAttemptId;
        LastResult = lastResult;
        Reason = reason;
        Exception = exception;
    }

    /// <summary>获取最早接纳且尚未退出的执行身份，空值表示没有运行中的操作。</summary>
    public Guid? RunningId => RunningIds.IsEmpty ? null : RunningIds[0];

    /// <summary>获取所有仍在执行或等待所属工作退出的身份，按接纳顺序排列。</summary>
    public ImmutableList<Guid> RunningIds { get; }

    /// <summary>获取仍在执行或等待所属工作退出的数量。</summary>
    public int RunningCount => RunningIds.Count;

    /// <summary>获取操作是否仍在执行或等待所属工作退出。</summary>
    public bool IsRunning => !RunningIds.IsEmpty;

    /// <summary>获取最近发布反馈所关联的调用身份。</summary>
    public Guid LastAttemptId { get; }

    /// <summary>获取最近发布的执行反馈，空值表示刚刚获准启动。</summary>
    public OperationResultKind? LastResult { get; }

    /// <summary>获取最近发布反馈的可见拒绝或故障原因。</summary>
    public string? Reason { get; }

    /// <summary>获取最近发布反馈的故障异常。</summary>
    public Exception? Exception { get; }
}
