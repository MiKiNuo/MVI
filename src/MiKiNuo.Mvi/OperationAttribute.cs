namespace MiKiNuo.Mvi;

/// <summary>声明同名业务操作的并发接纳方式。</summary>
public enum OperationConcurrency
{
    /// <summary>已有执行时拒绝重复调用。</summary>
    Reject,
    /// <summary>新执行取代旧执行的反馈身份，并请求旧执行协作取消。</summary>
    Latest,
    /// <summary>将调用放入容量有限的顺序等待队列。</summary>
    Queue,
    /// <summary>按明确声明的有限上限接纳独立执行。</summary>
    Parallel,
}

/// <summary>声明由统一入口接纳并在后台执行的业务操作。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationAttribute : Attribute
{
    /// <summary>获取或设置启动时依据当前状态执行的纯验证方法名称。</summary>
    public string? Validate { get; set; }

    /// <summary>获取或设置同名操作的并发策略，默认拒绝重复执行。</summary>
    public OperationConcurrency Concurrency { get; set; }

    /// <summary>获取或设置 Queue 的等待名额，选择 Queue 时必须为正数，运行项不占此容量。</summary>
    public int Capacity { get; set; }

    /// <summary>获取或设置并行操作的正数上限；零表示未声明并行上限。</summary>
    public int MaxConcurrency { get; set; }
}
