namespace MiKiNuo.Mvi.Abstractions.Generation;

/// <summary>
/// 标记 Mediator 的请求响应路由。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MviRouteAttribute : Attribute
{
    /// <summary>
    /// 初始化 Mediator 路由特性。
    /// </summary>
    /// <param name="requestType">请求类型。</param>
    /// <param name="responseType">响应类型。</param>
    public MviRouteAttribute(Type requestType, Type responseType)
    {
        RequestType = requestType;
        ResponseType = responseType;
    }

    /// <summary>
    /// 获取请求类型。
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// 获取响应类型。
    /// </summary>
    public Type ResponseType { get; }
}
