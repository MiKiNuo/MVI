using MiKiNuo.Mvi.Application.MVI.Mediator;
using TUnit.Assertions;
using MiKiNuo.Mvi.Domain.MVI.Mediator;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>
/// 表示框架级 MviMediator 的行为测试。
/// </summary>
public sealed class MviMediatorTests
{
    /// <summary>
    /// 验证注册处理器后可按请求类型路由并返回响应。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_RouteToRegisteredHandlerAsync()
    {
        MviMediator mediator = new();
        mediator.Register<PingRequest, PongResponse>(new PingHandler());
        PongResponse response = await mediator.SendAsync<PongResponse>(new PingRequest("hello"));
        await Assert.That(response.Message).IsEqualTo("pong:hello");
    }
    /// <summary>
    /// 验证委托注册重载可用。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_SupportDelegateRegistrationAsync()
    {
        MviMediator mediator = new();
        mediator.Register<PingRequest, PongResponse>(
            (request, _) => ValueTask.FromResult(new PongResponse("direct:" + request.Message)));
        PongResponse response = await mediator.SendAsync<PongResponse>(new PingRequest("x"));
        await Assert.That(response.Message).IsEqualTo("direct:x");
    }
    /// <summary>
    /// 验证未注册请求类型抛出路由未找到异常。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Throw_WhenRouteNotRegisteredAsync()
    {
        MviMediator mediator = new();
        await Assert.That(async () =>
            await mediator.SendAsync<PongResponse>(new PingRequest("x")))
            .Throws<MviMediatorRouteNotFoundException>();
    }
    /// <summary>
    /// 验证同一请求类型重复注册立即失败。
    /// </summary>
    [Test]
    public async Task Register_Should_Throw_WhenRouteDuplicatedAsync()
    {
        MviMediator mediator = new();
        mediator.Register<PingRequest, PongResponse>(new PingHandler());
        await Assert.That(() => mediator.Register<PingRequest, PongResponse>(new PingHandler()))
            .Throws<InvalidOperationException>();
    }
    /// <summary>
    /// 表示测试用 Ping 请求。
    /// </summary>
    /// <param name="Message">消息内容。</param>
    private sealed record PingRequest(string Message) : IMviRequest<PongResponse>;
    /// <summary>
    /// 表示测试用 Pong 响应。
    /// </summary>
    /// <param name="Message">消息内容。</param>
    private sealed record PongResponse(string Message);
    /// <summary>
    /// 表示测试用 Ping 处理器。
    /// </summary>
    private sealed class PingHandler : IMviRequestHandler<PingRequest, PongResponse>
    {
        /// <summary>
        /// 处理 Ping 请求。
        /// </summary>
        /// <param name="request">请求对象。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>Pong 响应。</returns>
        public async ValueTask<PongResponse> HandleAsync(
            PingRequest request,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new PongResponse("pong:" + request.Message);
        }
    }
}
