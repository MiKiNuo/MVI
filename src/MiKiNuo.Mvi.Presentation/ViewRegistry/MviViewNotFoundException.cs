namespace MiKiNuo.Mvi.Presentation.ViewRegistry;

/// <summary>
/// 表示视图未找到异常。
/// </summary>
public sealed class MviViewNotFoundException : Exception
{
    /// <summary>
    /// 初始化视图未找到异常。
    /// </summary>
    /// <param name="viewModelType">视图模型类型。</param>
    public MviViewNotFoundException(Type viewModelType)
        : base(CreateMessage(viewModelType))
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        ViewModelType = viewModelType;
    }

    /// <summary>
    /// 获取视图模型类型。
    /// </summary>
    public Type ViewModelType { get; }

    private static string CreateMessage(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        return $"未找到 ViewModel 对应的 View：{viewModelType.FullName}";
    }
}
