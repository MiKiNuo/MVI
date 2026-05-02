namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 标记当前 ViewModel 属性从 State 属性单向绑定生成。
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class MviStateBindingAttribute : Attribute
{
    /// <summary>
    /// 初始化 State 单向绑定特性。
    /// </summary>
    /// <param name="statePropertyName">State 属性名称。</param>
    public MviStateBindingAttribute(string statePropertyName)
    {
        StatePropertyName = statePropertyName;
    }

    /// <summary>
    /// 获取 State 属性名称。
    /// </summary>
    public string StatePropertyName { get; }
}
