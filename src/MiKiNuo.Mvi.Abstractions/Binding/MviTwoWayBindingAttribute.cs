namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 标记当前 ViewModel 属性支持双向绑定。
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MviTwoWayBindingAttribute : Attribute
{
    /// <summary>
    /// 初始化双向绑定特性。
    /// </summary>
    /// <param name="statePropertyName">State 属性名称。</param>
    /// <param name="intentType">属性变化时派发的 Intent 类型。</param>
    public MviTwoWayBindingAttribute(string statePropertyName, Type intentType)
    {
        StatePropertyName = statePropertyName;
        IntentType = intentType;
    }

    /// <summary>
    /// 获取 State 属性名称。
    /// </summary>
    public string StatePropertyName { get; }

    /// <summary>
    /// 获取属性变化时派发的 Intent 类型。
    /// </summary>
    public Type IntentType { get; }

    /// <summary>
    /// 获取双向绑定更新模式。
    /// </summary>
    public MviTwoWayUpdateMode UpdateMode { get; set; } = MviTwoWayUpdateMode.StateFirst;
}
