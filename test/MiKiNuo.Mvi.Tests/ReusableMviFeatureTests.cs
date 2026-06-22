using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示可复用 MVI Feature 测试。
/// </summary>
public sealed class ReusableMviFeatureTests
{
    /// <summary>
    /// 验证患者检索 MVI 可以携带不同页面键复用到多个业务页面。
    /// </summary>
    [Test]
    public async Task PatientSearchFeature_Should_BeReusableAcrossBusinessPagesAsync()
    {
        PatientSearchState inpatientState = PatientSearchState.CreateInitial("住院床位");
        PatientSearchState labState = PatientSearchState.CreateInitial("检验医嘱");

        await Assert.That(inpatientState.PageKey).IsEqualTo("住院床位");
        await Assert.That(labState.PageKey).IsEqualTo("检验医嘱");
        await Assert.That(inpatientState.GetType()).IsEqualTo(labState.GetType());
    }

    /// <summary>
    /// 验证患者检索副作用通过 Mediator 传输患者上下文。
    /// </summary>
    [Test]
    public async Task PatientSearchEffect_Should_SendPatientContextThroughMediatorAsync()
    {
        RecordingMediator mediator = new();
        PatientSearchEffectDispatcher dispatcher = new(mediator);

        await dispatcher.DispatchAsync(new PatientSearchEffect.RequestPatientContext("住院床位", "张三", "P10001"));

        await Assert.That(mediator.LastRequest).IsNotNull();
        await Assert.That(mediator.LastRequest!.PageKey).IsEqualTo("住院床位");
        await Assert.That(mediator.LastRequest.SourceComponent).IsEqualTo("住院床位/患者检索 MVI");
    }

    private sealed class RecordingMediator : IMviMediator
    {
        /// <summary>
        /// 获取最后一次 Dashboard 组件交互请求。
        /// </summary>
        public DashboardComponentInteractionRequest? LastRequest { get; private set; }

        /// <summary>
        /// 异步发送请求并返回响应。
        /// </summary>
        /// <typeparam name="TRequest">请求类型。</typeparam>
        /// <typeparam name="TResponse">响应类型。</typeparam>
        /// <param name="request">请求实例。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>响应实例。</returns>
        public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : notnull
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            if (request is DashboardComponentInteractionRequest interactionRequest)
            {
                LastRequest = interactionRequest;
                object response = new DashboardComponentInteractionResponse("已记录患者上下文", true);
                return ValueTask.FromResult((TResponse)response);
            }

            throw new InvalidOperationException("测试中介者未注册该请求。");
        }
    }
}
