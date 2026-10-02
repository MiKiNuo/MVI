namespace MiKiNuo.Mvi;

/// <summary>声明由统一入口接纳并在后台执行的业务操作。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationAttribute : Attribute
{
    /// <summary>获取或设置启动时依据当前状态执行的纯验证方法名称。</summary>
    public string? Validate { get; set; }

    /// <summary>获取或设置同名操作的并发准入规则，默认拒绝重复启动。</summary>
    public OperationConcurrency Concurrency { get; set; }
}

/// <summary>选择同一实例内同名操作的执行准入规则。</summary>
public enum OperationConcurrency
{
    /// <summary>已有有效执行时拒绝新的调用。</summary>
    Reject,
    /// <summary>新执行取代旧执行的反馈身份，并请求旧执行协作取消。</summary>
    Latest,
}
