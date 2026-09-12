namespace MiKiNuo.Mvi.Application.MVI.Mediator;

using MiKiNuo.Mvi.Domain.MVI.Mediator;

/// <summary>绑定来源实例与组合范围的通信端点。</summary>
public sealed class MviMediatorEndpoint : IMviMediator, IDisposable
{
    /// <summary>所属组合范围。</summary>
    private readonly MviCompositionScope _scope;

    /// <summary>初始化实例端点。</summary>
    /// <param name="scope">所属范围。</param>
    /// <param name="instanceId">实例标识。</param>
    internal MviMediatorEndpoint(MviCompositionScope scope, Guid instanceId)
    {
        _scope = scope;
        InstanceId = instanceId;
    }

    /// <summary>获取实例标识。</summary>
    public Guid InstanceId { get; }

    /// <summary>向绑定目标发送请求。</summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求契约。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理器返回的业务响应。</returns>
    public ValueTask<TResponse> SendAsync<TResponse>(IMviRequest<TResponse> request, CancellationToken cancellationToken = default)
        => _scope.SendAsync(InstanceId, request, cancellationToken);

    /// <summary>发布事实并返回同步接纳明细，不等待后续副作用。</summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">通知契约。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>快照内所有订阅的投递报告。</returns>
    public ValueTask<MviNotificationReport> PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : IMviNotification
        => _scope.PublishAsync(InstanceId, notification, cancellationToken);

    /// <summary>注销实例端点。</summary>
    public void Dispose() => _scope.Remove(InstanceId);
}
