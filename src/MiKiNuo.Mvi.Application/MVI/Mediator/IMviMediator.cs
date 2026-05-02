namespace MiKiNuo.Mvi.Application.MVI.Mediator;

/// <summary>
/// 表示真正的 Request/Response 中介者。
/// </summary>
public interface IMviMediator
{
    /// <summary>
    /// 发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>响应对象。</returns>
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;
}
