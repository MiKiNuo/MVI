namespace MiKiNuo.Mvi.Application.MVI.Mediator;

/// <summary>
/// 表示中介者路由未找到异常。
/// </summary>
public sealed class MviMediatorRouteNotFoundException : Exception
{
    /// <summary>
    /// 初始化中介者路由未找到异常。
    /// </summary>
    /// <param name="requestType">请求类型。</param>
    /// <param name="responseType">响应类型。</param>
    public MviMediatorRouteNotFoundException(Type requestType, Type responseType)
        : base(CreateMessage(requestType, responseType))
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
    }

    /// <summary>
    /// 获取请求类型。
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// 获取响应类型。
    /// </summary>
    public Type ResponseType { get; }

    private static string CreateMessage(Type requestType, Type responseType)
    {
        if (requestType is null)
        {
            throw new ArgumentNullException(nameof(requestType));
        }

        if (responseType is null)
        {
            throw new ArgumentNullException(nameof(responseType));
        }

        return $"未找到中介者路由：{requestType.FullName} -> {responseType.FullName}";
    }
}
