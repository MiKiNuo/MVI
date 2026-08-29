namespace MiKiNuo.Mvi.Application.MVI.Mediator;

using MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>
/// 表示真正的 Request/Response 中介者。
/// </summary>
public interface IMviMediator
{
    /// <summary>
    /// 发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>响应对象。</returns>
    public ValueTask<TResponse> SendAsync<TResponse>(
        IMviRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
