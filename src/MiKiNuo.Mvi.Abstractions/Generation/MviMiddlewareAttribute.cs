namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记中间件在生成管线中的顺序。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MviMiddlewareAttribute : Attribute
{
    /// <summary>
    /// 初始化中间件顺序特性。
    /// </summary>
    /// <param name="order">中间件顺序。</param>
    public MviMiddlewareAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// 获取中间件顺序。
    /// </summary>
    public int Order { get; }
}
