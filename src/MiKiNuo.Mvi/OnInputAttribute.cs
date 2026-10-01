namespace MiKiNuo.Mvi;

/// <summary>指定某个输入使用的纯状态转换。</summary>
/// <param name="propertyName">带有输入标记的状态属性名。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class OnInputAttribute(string propertyName) : Attribute
{
    /// <summary>获取需要转换的状态属性名。</summary>
    public string PropertyName { get; } = propertyName;
}
