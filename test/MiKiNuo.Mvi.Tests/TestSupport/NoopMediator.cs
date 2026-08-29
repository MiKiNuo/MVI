using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Mediator;

namespace MiKiNuo.Mvi.Tests.TestSupport;

/// <summary>
/// 表示空操作中介者测试桩，所有请求返回默认响应。
/// </summary>
internal sealed class NoopMediator : IMviMediator
{
    /// <summary>
    /// 始终返回 TResponse 默认实例；不派发任何请求。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象（忽略）。</param>
    /// <param name="cancellationToken">取消标记（忽略）。</param>
    /// <returns>默认响应实例。</returns>
    public ValueTask<TResponse> SendAsync<TResponse>(
        IMviRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<TResponse>(default(TResponse)!);
    }
}
