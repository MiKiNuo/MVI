namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记 View 与 ViewModel 的编译期映射。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MviViewRegistryAttribute : Attribute
{
    /// <summary>
    /// 初始化 View 注册特性。
    /// </summary>
    /// <param name="viewType">View 类型。</param>
    /// <param name="viewModelType">ViewModel 类型。</param>
    public MviViewRegistryAttribute(Type viewType, Type viewModelType)
    {
        ViewType = viewType;
        ViewModelType = viewModelType;
    }

    /// <summary>
    /// 获取 View 类型。
    /// </summary>
    public Type ViewType { get; }

    /// <summary>
    /// 获取 ViewModel 类型。
    /// </summary>
    public Type ViewModelType { get; }
}
