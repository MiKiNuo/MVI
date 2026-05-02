namespace MiKiNuo.Mvi.Abstractions.Binding;

/// <summary>
/// 标记当前命令创建 Intent 时的构造参数来源。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class MviCommandArgumentAttribute : Attribute
{
    /// <summary>
    /// 初始化命令参数映射特性。
    /// </summary>
    /// <param name="intentParameterName">Intent 构造函数参数名称。</param>
    /// <param name="viewModelPropertyName">ViewModel 属性名称。</param>
    public MviCommandArgumentAttribute(string intentParameterName, string viewModelPropertyName)
    {
        IntentParameterName = intentParameterName;
        ViewModelPropertyName = viewModelPropertyName;
    }

    /// <summary>
    /// 获取 Intent 构造函数参数名称。
    /// </summary>
    public string IntentParameterName { get; }

    /// <summary>
    /// 获取 ViewModel 属性名称。
    /// </summary>
    public string ViewModelPropertyName { get; }
}
