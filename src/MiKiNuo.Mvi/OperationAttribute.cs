namespace MiKiNuo.Mvi;

/// <summary>声明由统一入口接纳并在后台执行的业务操作。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationAttribute : Attribute
{
    /// <summary>获取或设置启动时依据当前状态执行的纯验证方法名称。</summary>
    public string? Validate { get; set; }

    /// <summary>获取或设置同名操作的接纳策略，默认拒绝重复启动。</summary>
    public OperationConcurrency Concurrency { get; set; }

    /// <summary>获取或设置并行操作的正数上限；零表示未声明并行上限。</summary>
    public int MaxConcurrency { get; set; }
}

/// <summary>选择同一实例内同名操作的接纳策略。</summary>
public enum OperationConcurrency
{
    /// <summary>仍有执行时拒绝新的同名调用。</summary>
    Reject,
    /// <summary>按明确声明的有限上限接纳独立执行。</summary>
    Parallel,
}
