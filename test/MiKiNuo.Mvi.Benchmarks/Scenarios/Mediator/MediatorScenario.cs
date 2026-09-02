using MiKiNuo.Mvi.Application.MVI.Mediator;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mediator;

/// <summary>
/// 表示中介者路由规模扫描场景：
/// 在 1 条与 300 条路由两种规模下发送链尾目标请求，
/// 与 DI 深度扫描对照回答"哈希查表是否随表大小退化"。
/// </summary>
public sealed class MediatorScenario
{
    private readonly MviMediator _mediator = new();
    private readonly MediatorRequest299 _request = new(SyntheticMediatorRouteCatalog.SyntheticRouteCount - 1);
    private readonly Func<MediatorRequest299, CancellationToken, ValueTask<MediatorResponse>> _directHandler =
        static (request, _) => ValueTask.FromResult(new MediatorResponse(request.Value));

    /// <summary>
    /// 初始化中介者基准场景并按规模注册路由。
    /// </summary>
    /// <param name="routeCount">路由规模：1（仅目标路由）或 <see cref="SyntheticMediatorRouteCatalog.SyntheticRouteCount"/>（全部路由）。</param>
    /// <exception cref="ArgumentOutOfRangeException">路由规模不受支持时抛出。</exception>
    public MediatorScenario(int routeCount)
    {
        switch (routeCount)
        {
            case 1:
                SyntheticMediatorRouteCatalog.RegisterTargetRoute(_mediator);
                break;
            case SyntheticMediatorRouteCatalog.SyntheticRouteCount:
                SyntheticMediatorRouteCatalog.RegisterAllRoutes(_mediator);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(routeCount),
                    $"仅支持 1 或 {SyntheticMediatorRouteCatalog.SyntheticRouteCount} 条路由规模，实际为 {routeCount}。");
        }

        RouteCount = routeCount;
    }

    /// <summary>
    /// 获取已注册的路由数量。
    /// </summary>
    public int RouteCount { get; }

    /// <summary>
    /// 经中介者发送链尾目标请求。
    /// </summary>
    /// <returns>目标请求的响应。</returns>
    public ValueTask<MediatorResponse> SendAsync()
    {
        return _mediator.SendAsync<MediatorResponse>(_request);
    }

    /// <summary>
    /// 绕过中介者直接调用处理委托（对照基线）。
    /// </summary>
    /// <returns>目标请求的响应。</returns>
    public ValueTask<MediatorResponse> SendDirectAsync()
    {
        return _directHandler(_request, CancellationToken.None);
    }
}
