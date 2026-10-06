using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Runtime.MVI.Intent;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证示例公开入口先校验参数，不在审计或业务处理中产生空引用异常。</summary>
public sealed class SampleArgumentValidationTests
{
    /// <summary>空意图在调用下一步之前被拒绝，返回正确的参数名。</summary>
    [Test]
    public async Task AuditRejectsNullIntentBeforeNextStepAsync()
    {
        int calls = 0;
        LoginAuditMiddleware middleware = new();
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(null!, new UnusedLoginContext(),
            () => { calls++; return ValueTask.CompletedTask; }, default), "intent");
        await Assert.That(calls).IsEqualTo(0);
    }

    /// <summary>上下文同样属于必需的中间件契约，缺失时不能继续管线。</summary>
    [Test]
    public async Task AuditRejectsNullContextBeforeNextStepAsync()
    {
        int calls = 0;
        LoginAuditMiddleware middleware = new();
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(new LoginIntent.GoRegister(), null!,
            () => { calls++; return ValueTask.CompletedTask; }, default), "context");
        await Assert.That(calls).IsEqualTo(0);
    }

    /// <summary>空下一步委托被明确拒绝，不会以空引用异常结束。</summary>
    [Test]
    public async Task AuditRejectsNullNextStepAsync()
    {
        LoginAuditMiddleware middleware = new();
        await ExpectNullArgumentAsync(() => middleware.InvokeAsync(new LoginIntent.GoRegister(), new UnusedLoginContext(),
            null!, default), "nextStep");
    }

    /// <summary>合法输入仍然只执行一次下一步，审计中间件不访问或修改状态。</summary>
    [Test]
    public async Task AuditExecutesValidNextStepOnceAsync()
    {
        int calls = 0;
        LoginAuditMiddleware middleware = new();
        await middleware.InvokeAsync(new LoginIntent.GoRegister(), new UnusedLoginContext(),
            () => { calls++; return ValueTask.CompletedTask; }, default);
        await Assert.That(calls).IsEqualTo(1);
    }

    /// <summary>空大厅通知在读取显示名及尝试投递之前被拒绝。</summary>
    [Test]
    public async Task HomeRejectsNullNotificationAsync()
    {
        HomeHandler handler = new(new NoopMediator());
        await ExpectNullArgumentAsync(() =>
        {
            handler.OnHomeEntered(null!);
            return ValueTask.CompletedTask;
        }, "notification");
    }

    private static async Task ExpectNullArgumentAsync(Func<ValueTask> action, string parameterName)
    {
        try
        {
            await action();
        }
        catch (ArgumentNullException exception)
        {
            await Assert.That(exception.ParamName).IsEqualTo(parameterName);
            return;
        }
        throw new InvalidOperationException("预期拒绝空参数：" + parameterName);
    }

    private sealed class UnusedLoginContext : IIntentContext<LoginState>
    {
        /// <inheritdoc/>
        public LoginState State => throw new InvalidOperationException("审计不应读取状态。");
        /// <inheritdoc/>
        public void Reduce(IMviMutation<LoginState> mutation) => throw new InvalidOperationException("审计不应修改状态。");
        /// <inheritdoc/>
        public bool TryReduce(Func<LoginState, bool> guard, IMviMutation<LoginState> mutation)
            => throw new InvalidOperationException("审计不应修改状态。");
    }
}
