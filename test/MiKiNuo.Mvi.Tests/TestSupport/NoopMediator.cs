using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Mediator;

namespace MiKiNuo.Mvi.Tests.TestSupport;

/// <summary>
/// 表示空操作中介者测试桩，所有请求返回默认响应。
/// </summary>
internal sealed class NoopMediator : IMviMediator
{
    /// <summary>
    /// 始终返回 TResponse 默认实例；不派发任何请求。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象（忽略）。</param>
    /// <param name="cancellationToken">取消标记（忽略）。</param>
    /// <returns>默认响应实例。</returns>
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        return new ValueTask<TResponse>(default(TResponse)!);
    }
}
