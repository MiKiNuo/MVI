namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 标记当前命令属性由指定 Intent 自动生成。
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MviCommandAttribute : Attribute
{
    /// <summary>
    /// 初始化 MVI 命令绑定特性。
    /// </summary>
    /// <param name="intentType">命令执行时派发的 Intent 类型。</param>
    public MviCommandAttribute(Type intentType)
    {
        IntentType = intentType;
    }

    /// <summary>
    /// 获取命令执行时派发的 Intent 类型。
    /// </summary>
    public Type IntentType { get; }

    /// <summary>
    /// 获取控制命令是否可执行的 State 属性名称。
    /// </summary>
    public string? CanExecuteStatePropertyName { get; set; }

    /// <summary>
    /// 获取命令参数类型。
    /// </summary>
    public Type? ParameterType { get; set; }
}
