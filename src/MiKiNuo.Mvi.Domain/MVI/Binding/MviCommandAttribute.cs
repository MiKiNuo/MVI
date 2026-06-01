namespace MiKiNuo.Mvi.Domain.MVI.Binding;

/// <summary>
/// 表示 ViewModel 命令与 Intent 之间的绑定元数据。
/// </summary>
/// <param name="intentType">命令触发时需要派发的 Intent 类型。</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MviCommandAttribute(Type intentType) : Attribute
{
    /// <summary>
    /// 获取命令触发时需要派发的 Intent 类型。
    /// </summary>
    public Type IntentType { get; } = intentType;

    /// <summary>
    /// 获取或设置可执行状态对应的 ViewModel 属性名称。
    /// </summary>
    public string? CanExecuteProperty { get; set; }

    /// <summary>
    /// 获取或设置是否异步命令。
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// 获取或设置命令载荷类型，用于存在多个一参 Intent 构造函数时消除歧义。
    /// </summary>
    public Type? PayloadType { get; set; }
}
