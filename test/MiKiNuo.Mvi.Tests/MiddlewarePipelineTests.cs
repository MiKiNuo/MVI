using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.Common.Errors;
using MiKiNuo.Mvi.Domain.Common.Results;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 中间件管道测试。
/// </summary>
public sealed class MiddlewarePipelineTests
{
    /// <summary>
    /// 验证中间件可以记录 Intent、校验结果和性能耗时。
    /// </summary>
    [Test]
    public async Task MiddlewarePipeline_Should_RecordIntentValidationAndPerformanceAsync()
    {
        using MviMemoryDiagnosticSink diagnosticSink = new();
        IReadOnlyList<IMviMiddleware<LoginState, LoginIntent, LoginEffect>> middlewares =
        [
            new MviValidationMiddleware<LoginState, LoginIntent, LoginEffect>(
                static (state, intent) => intent is LoginIntent.Submit && string.IsNullOrWhiteSpace(state.UserName)
                    ? OperationResult.Failure(new DomainError("Login.UserNameEmpty", "用户名不能为空"))
                    : OperationResult.Success(),
                diagnosticSink,
                "登录测试 MVI"),
            new MviLoggingMiddleware<LoginState, LoginIntent, LoginEffect>(diagnosticSink, "登录测试 MVI"),
            new MviPerformanceMiddleware<LoginState, LoginIntent, LoginEffect>(diagnosticSink, "登录测试 MVI")
        ];

        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginReducer(),
            new NoopEffectDispatcher<LoginEffect>(),
            middlewares);

        await store.DispatchAsync(new LoginIntent.Submit());
        await Assert.That(store.CurrentState.IsBusy).IsFalse();

        await store.DispatchAsync(new LoginIntent.ChangeUserName("emilys"));
        await store.DispatchAsync(new LoginIntent.ChangePassword("emilyspass"));
        await store.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsTrue();
        await Assert.That(diagnosticSink.Entries.Count > 0).IsTrue();
        await Assert.That(diagnosticSink.Entries.Any(static entry => entry.Stage == "Validation")).IsTrue();
        await Assert.That(diagnosticSink.Entries.Any(static entry => entry.Stage == "Middleware")).IsTrue();
    }

    /// <summary>
    /// 验证管线在构造期快照中间件集合：构造后向原集合追加的中间件不参与后续派发。
    /// </summary>
    [Test]
    public async Task Pipeline_Should_SnapshotMiddlewareCollectionAtConstructionAsync()
    {
        List<string> order = [];
        List<IMviMiddleware<PipelineTestState, PipelineTestIntent, PipelineTestEffect>> middlewares =
        [
            new RecordingMiddleware(order, "first"),
        ];

        MviMiddlewarePipeline<PipelineTestState, PipelineTestIntent, PipelineTestEffect> pipeline =
            new(middlewares, CreateTerminalStep(order));

        middlewares.Add(new RecordingMiddleware(order, "late"));

        MviReduceResult<PipelineTestState, PipelineTestEffect> result = await pipeline.InvokeAsync(
            new MviMiddlewareContext<PipelineTestState, PipelineTestIntent, PipelineTestEffect>(
                PipelineTestState.Initial,
                new PipelineTestIntent.Nop()),
            CancellationToken.None);

        await Assert.That(order.Count).IsEqualTo(2);
        await Assert.That(order[0]).IsEqualTo("first");
        await Assert.That(order[1]).IsEqualTo("terminal");
        await Assert.That(result.State.Counter).IsEqualTo(0);
    }

    /// <summary>
    /// 验证管线按注册顺序调用中间件并最终到达终端规约步骤。
    /// </summary>
    [Test]
    public async Task Pipeline_Should_InvokeMiddlewaresInOrderBeforeTerminalAsync()
    {
        List<string> order = [];
        MviMiddlewarePipeline<PipelineTestState, PipelineTestIntent, PipelineTestEffect> pipeline = new(
            new IMviMiddleware<PipelineTestState, PipelineTestIntent, PipelineTestEffect>[]
            {
                new RecordingMiddleware(order, "first"),
                new RecordingMiddleware(order, "second"),
                new RecordingMiddleware(order, "third"),
            },
            CreateTerminalStep(order));

        await pipeline.InvokeAsync(
            new MviMiddlewareContext<PipelineTestState, PipelineTestIntent, PipelineTestEffect>(
                PipelineTestState.Initial,
                new PipelineTestIntent.Nop()),
            CancellationToken.None);

        await Assert.That(order.Count).IsEqualTo(4);
        await Assert.That(order[0]).IsEqualTo("first");
        await Assert.That(order[1]).IsEqualTo("second");
        await Assert.That(order[2]).IsEqualTo("third");
        await Assert.That(order[3]).IsEqualTo("terminal");
    }

    /// <summary>
    /// 验证空中间件集合时管线直接调用终端规约步骤。
    /// </summary>
    [Test]
    public async Task Pipeline_WithEmptyMiddlewares_Should_InvokeTerminalDirectlyAsync()
    {
        List<string> order = [];
        MviMiddlewarePipeline<PipelineTestState, PipelineTestIntent, PipelineTestEffect> pipeline =
            new(null, CreateTerminalStep(order));

        await pipeline.InvokeAsync(
            new MviMiddlewareContext<PipelineTestState, PipelineTestIntent, PipelineTestEffect>(
                PipelineTestState.Initial,
                new PipelineTestIntent.Nop()),
            CancellationToken.None);

        await Assert.That(order.Count).IsEqualTo(1);
        await Assert.That(order[0]).IsEqualTo("terminal");
    }

    /// <summary>
    /// 验证终端委托为 null 时构造函数立即抛出参数异常。
    /// </summary>
    [Test]
    public async Task Pipeline_NullTerminal_Should_ThrowAtConstructionAsync()
    {
        await Assert.That(() => new MviMiddlewarePipeline<PipelineTestState, PipelineTestIntent, PipelineTestEffect>(
            null,
            null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// 创建记录终端调用的规约终端步骤。
    /// </summary>
    /// <param name="order">调用顺序记录。</param>
    /// <returns>终端规约委托。</returns>
    private static MviMiddlewareStep<PipelineTestState, PipelineTestIntent, PipelineTestEffect> CreateTerminalStep(
        List<string> order)
    {
        return (context, cancellationToken) =>
        {
            order.Add("terminal");
            return ValueTask.FromResult(
                MviReduceResult.State<PipelineTestState, PipelineTestEffect>(context.State));
        };
    }

    /// <summary>
    /// 表示管线测试用最小状态。
    /// </summary>
    /// <param name="Counter">已完成的意图计数。</param>
    private sealed record PipelineTestState(int Counter) : IMviState
    {
        /// <summary>
        /// 获取初始状态。
        /// </summary>
        public static PipelineTestState Initial { get; } = new(0);
    }

    /// <summary>
    /// 表示管线测试用最小意图。
    /// </summary>
    private abstract record PipelineTestIntent : IMviIntent
    {
        /// <summary>
        /// 表示无操作意图。
        /// </summary>
        internal sealed record Nop : PipelineTestIntent;
    }

    /// <summary>
    /// 表示管线测试用最小副作用。
    /// </summary>
    private abstract record PipelineTestEffect : IMviEffect;

    /// <summary>
    /// 表示记录调用顺序的直通中间件。
    /// </summary>
    /// <param name="Order">调用顺序记录。</param>
    /// <param name="Name">中间件名称。</param>
    private sealed class RecordingMiddleware(List<string> Order, string Name)
        : IMviMiddleware<PipelineTestState, PipelineTestIntent, PipelineTestEffect>
    {
        /// <summary>
        /// 记录自身调用后直通下一层。
        /// </summary>
        /// <param name="context">中间件上下文。</param>
        /// <param name="nextMiddleware">下一层中间件。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>下一层产出的规约结果。</returns>
        public async ValueTask<MviReduceResult<PipelineTestState, PipelineTestEffect>> InvokeAsync(
            MviMiddlewareContext<PipelineTestState, PipelineTestIntent, PipelineTestEffect> context,
            MviMiddlewareStep<PipelineTestState, PipelineTestIntent, PipelineTestEffect> nextMiddleware,
            CancellationToken cancellationToken)
        {
            Order.Add(Name);
            return await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
