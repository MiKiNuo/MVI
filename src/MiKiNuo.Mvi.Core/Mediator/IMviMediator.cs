namespace MiKiNuo.Mvi.Core.Mediator;

/// <summary>
/// 表示 MVI 组件协调器。
/// </summary>
public interface IMviMediator
{
    /// <summary>
    /// 向目标组件发送请求并返回响应。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="target">目标组件地址。</param>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>响应对象。</returns>
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        ComponentAddress target,
        TRequest request,
        CancellationToken cancellationToken);
}
