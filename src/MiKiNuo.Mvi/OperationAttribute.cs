namespace MiKiNuo.Mvi;

/// <summary>声明由统一入口接纳并在后台执行的业务操作。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperationAttribute : Attribute
{
    /// <summary>获取或设置启动时依据当前状态执行的纯验证方法名称。</summary>
    public string? Validate { get; set; }
}
