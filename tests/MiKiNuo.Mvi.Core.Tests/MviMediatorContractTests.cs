using MiKiNuo.Mvi.Core.Mediator;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviMediatorContractTests
{
    [Test]
    public async Task ComponentAddress_root_uses_stable_root_value()
    {
        await Assert.That(ComponentAddress.Root.Value).IsEqualTo("root");
    }

    [Test]
    public async Task Mediator_send_async_returns_typed_response()
    {
        IMviMediator mediator = new DelegateMediator((target, request, _) =>
        {
            if (target == ComponentAddress.Root && request is PingRequest ping)
            {
                return ValueTask.FromResult<object>(new PingResponse($"pong:{ping.Message}"));
            }

            throw new InvalidOperationException("unexpected request");
        });

        var response = await mediator.SendAsync<PingRequest, PingResponse>(
            ComponentAddress.Root,
            new PingRequest("hello"),
            CancellationToken.None);

        await Assert.That(response).IsEqualTo(new PingResponse("pong:hello"));
    }

    private sealed record PingRequest(string Message);

    private sealed record PingResponse(string Message);

    private sealed class DelegateMediator : IMviMediator
    {
        private readonly Func<ComponentAddress, object, CancellationToken, ValueTask<object>> sendAsync;

        public DelegateMediator(Func<ComponentAddress, object, CancellationToken, ValueTask<object>> sendAsync)
        {
            this.sendAsync = sendAsync;
        }

        public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
            ComponentAddress target,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var response = await sendAsync(target, request!, cancellationToken);
            return (TResponse)response;
        }
    }
}
