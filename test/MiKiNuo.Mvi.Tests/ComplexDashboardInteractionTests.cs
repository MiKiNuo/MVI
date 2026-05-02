using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedOverview;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示复杂 Dashboard 子 MVI 交互测试。
/// </summary>
public sealed class ComplexDashboardInteractionTests
{
    /// <summary>
    /// 验证住院床位子组件执行主动作时会产生副作用请求。
    /// </summary>
    [Test]
    public async Task InpatientChildMvi_Should_EmitMediatorEffectAsync()
    {
        BedOverviewState state = BedOverviewState.Initial;

        MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<BedOverviewState, BedOverviewEffect> result =
            BedOverviewReducers.Reduce(state, new BedOverviewIntent.ExecutePrimaryAction());

        await Assert.That(result.Effects.Count).IsEqualTo(1);
        await Assert.That(result.Effects[0]).IsTypeOf<BedOverviewEffect.RequestPrimaryWorkflow>();
    }

    /// <summary>
    /// 验证副作用分发器会通过真正 Mediator Request/Response 传输数据。
    /// </summary>
    [Test]
    public async Task InpatientEffectDispatcher_Should_CallMediatorAsync()
    {
        RecordingMediator mediator = new();
        BedOverviewEffectDispatcher dispatcher = new(mediator);

        await dispatcher.DispatchAsync(new BedOverviewEffect.RequestPrimaryWorkflow("床位总览：急诊转入住院"));

        await Assert.That(mediator.LastRequest).IsNotNull();
        await Assert.That(mediator.LastRequest!.PageKey).IsEqualTo("住院床位");
        await Assert.That(mediator.LastRequest.SourceComponent).IsEqualTo("床位总览 MVI");
    }

    private sealed class RecordingMediator : IMviMediator
    {
        /// <summary>
        /// 获取最后一次请求。
        /// </summary>
        public DashboardComponentInteractionRequest? LastRequest { get; private set; }

        /// <inheritdoc />
        public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : notnull
        {
            if (request is DashboardComponentInteractionRequest interactionRequest)
            {
                LastRequest = interactionRequest;
                object response = new DashboardComponentInteractionResponse("已记录", true);
                return ValueTask.FromResult((TResponse)response);
            }

            throw new InvalidOperationException("测试中介者未注册该请求。 ");
        }
    }
}
