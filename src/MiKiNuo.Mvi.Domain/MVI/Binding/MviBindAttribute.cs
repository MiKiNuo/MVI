namespace MiKiNuo.Mvi.Domain.MVI.Binding;

/// <summary>
/// 表示 ViewModel 属性与 State 属性之间的绑定元数据。
/// </summary>
/// <param name="stateProperty">状态属性名称。</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MviBindAttribute(string stateProperty) : Attribute
{
    /// <summary>
    /// 获取状态属性名称。
    /// </summary>
    public string StateProperty { get; } = stateProperty;

    /// <summary>
    /// 获取或设置绑定模式。
    /// </summary>
    public MviBindingMode BindingMode { get; set; } = MviBindingMode.OneWay;

    /// <summary>
    /// 获取或设置双向绑定时生成的 Intent 类型。
    /// </summary>
    public Type? IntentType { get; set; }
}
