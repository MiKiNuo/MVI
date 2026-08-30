using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.Common.Errors;
using MiKiNuo.Mvi.Domain.Common.Results;
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
}
