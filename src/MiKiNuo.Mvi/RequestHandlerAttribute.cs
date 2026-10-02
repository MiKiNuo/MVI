namespace MiKiNuo.Mvi;

/// <summary>声明映射到本实例统一操作入口的强类型请求处理方法。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestHandlerAttribute : Attribute
{
    /// <summary>获取或设置根据当前状态和本次消息执行的纯验证方法名称。</summary>
    public string? Validate { get; set; }
    /// <summary>获取或设置同名目标操作的并发策略。</summary>
    public OperationConcurrency Concurrency { get; set; }
    /// <summary>获取或设置 Queue 的正数等待容量，其他策略必须为零。</summary>
    public int Capacity { get; set; }
    /// <summary>获取或设置 Parallel 的正数执行上限，其他策略必须为零。</summary>
    public int MaxConcurrency { get; set; }
    /// <summary>获取或设置目标执行取消策略，默认由目标拥有执行。</summary>
    public RequestCancellationPolicy CancellationPolicy { get; set; }
}
