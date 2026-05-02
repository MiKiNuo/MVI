namespace MiKiNuo.Mvi.Application.MVI.Mediator;

/// <summary>
/// 表示中介者请求处理器。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public interface IMviRequestHandler<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// 处理请求。
    /// </summary>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>响应对象。</returns>
    public ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
