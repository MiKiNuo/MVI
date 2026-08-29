namespace MiKiNuo.Mvi.Application.MVI.Mediator;

using MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>
/// 表示框架级 Request/Response 中介者实现。
/// </summary>
/// <remarks>
/// 路由表通过 <see cref="Register{TRequest, TResponse}(IMviRequestHandler{TRequest, TResponse})"/>
/// 显式注册，无反射、无运行时扫描，AOT 安全。
/// 每个请求类型只允许一个处理器，重复注册立即抛异常，
/// 保持“协调者而非事件总线”的语义。
/// </remarks>
public sealed class MviMediator : IMviMediator
{
    private readonly Dictionary<Type, Route> _routes = new();

    /// <summary>
    /// 注册请求类型的处理器。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="handler">请求处理器。</param>
    /// <returns>当前中介者实例，支持链式注册。</returns>
    /// <exception cref="InvalidOperationException">同一请求类型重复注册时抛出。</exception>
    public MviMediator Register<TRequest, TResponse>(IMviRequestHandler<TRequest, TResponse> handler)
        where TRequest : notnull, IMviRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Register<TRequest, TResponse>(handler.HandleAsync);
    }

    /// <summary>
    /// 注册请求类型的处理委托。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="handler">处理委托。</param>
    /// <returns>当前中介者实例，支持链式注册。</returns>
    /// <exception cref="InvalidOperationException">同一请求类型重复注册时抛出。</exception>
    public MviMediator Register<TRequest, TResponse>(
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler)
        where TRequest : notnull, IMviRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_routes.TryAdd(
            typeof(TRequest),
            new Route(
                typeof(TResponse),
                async (request, cancellationToken) =>
                    await handler((TRequest)request, cancellationToken).ConfigureAwait(false))))
        {
            throw new InvalidOperationException(
                $"中介者路由重复注册：{typeof(TRequest).FullName}。每个请求类型只允许一个处理器。");
        }

        return this;
    }

    /// <summary>
    /// 发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>响应对象。</returns>
    /// <exception cref="MviMediatorRouteNotFoundException">请求类型未注册路由时抛出。</exception>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        IMviRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        if (!_routes.TryGetValue(requestType, out Route? route))
        {
            throw new MviMediatorRouteNotFoundException(requestType, typeof(TResponse));
        }

        if (route.ResponseType != typeof(TResponse))
        {
            throw new InvalidOperationException(
                $"中介者路由响应类型不匹配：请求 {requestType.FullName} 注册为 {route.ResponseType.FullName}，调用期望 {typeof(TResponse).FullName}。");
        }

        object? response = await route.Handler(request, cancellationToken).ConfigureAwait(false);
        if (response is TResponse typedResponse)
        {
            return typedResponse;
        }

        if (response is null && default(TResponse) is null)
        {
            return default!;
        }

        throw new InvalidOperationException(
            $"中介者处理器返回类型不匹配：请求 {requestType.FullName} 期望 {typeof(TResponse).FullName}。");
    }

    private sealed record Route(
        Type ResponseType,
        Func<object, CancellationToken, ValueTask<object?>> Handler);
}
