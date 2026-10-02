using MiKiNuo.Mvi;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>验证三个 v2 认证表单的生成入口与已提交结果。</summary>
public sealed class V2AuthFlowTests
{
    /// <summary>UI 命令与程序入口都拒绝无效输入且不调用认证服务。</summary>
    /// <returns>启动验证任务。</returns>
    [Test]
    public async Task InvalidFormsRejectBothCommandAndProgramWithoutServiceCallsAsync()
    {
        ControlledAuthService service = new();
        LoginFeature login = new(service);
        RegisterFeature register = new(service);
        ResetPasswordFeature reset = new(service);
        using LoginFeature.Projection loginView = login.CreateProjection(static callback => callback());
        using RegisterFeature.Projection registerView = register.CreateProjection(static callback => callback());
        using ResetPasswordFeature.Projection resetView = reset.CreateProjection(static callback => callback());
        loginView.SubmitAsyncCommand.Execute(null);
        registerView.SubmitAsyncCommand.Execute(null);
        resetView.SubmitAsyncCommand.Execute(null);
        await Assert.That((await loginView.SubmitAsyncCommand.Execution!).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That((await registerView.SubmitAsyncCommand.Execution!).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That((await resetView.SubmitAsyncCommand.Execution!).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That((await login.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That((await register.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That((await reset.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(service.Calls).IsEqualTo(0);
        await Assert.That(register.Snapshot.State.ValidationError).IsEqualTo("用户名至少需要 3 个字符。");
        await Assert.That(register.Snapshot.OperationStates[nameof(RegisterFeature.SubmitAsync)].Reason).IsEqualTo("ValidationFailed");
    }

    /// <summary>慢服务使用启动时验证快照，业务反馈保留期间的新编辑，重复请求保留运行状态。</summary>
    /// <param name="form">被验收的认证表单。</param>
    /// <returns>慢服务完成契约验证任务。</returns>
    [Test]
    [Arguments("Login")]
    [Arguments("Register")]
    [Arguments("ResetPassword")]
    public async Task SlowServicePreservesEditsAndValidatedInputAsync(string form)
    {
        TaskCompletionSource<(string Name, string Password, string? Email)> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<AuthResult> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ControlledAuthService service = new()
        {
            Response = (name, password, email, _) => { started.SetResult((name, password, email)); return response.Task; }
        };
        AuthScenario scenario = CreateScenario(form, service);
        Task<OperationResult<AuthResult>> execution = scenario.Submit(CancellationToken.None);
        (string name, string password, string? email) = await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(name).IsEqualTo("neo");
        await Assert.That(password).IsEqualTo("abc123");
        await Assert.That(email).IsEqualTo(form == "Register" ? "neo@example.com" : null);
        scenario.Edit("new-edit");
        OperationResult<AuthResult> duplicate = await scenario.Submit(CancellationToken.None);
        await Assert.That(duplicate.Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(duplicate.Reason).IsEqualTo("AlreadyRunning");
        await Assert.That(scenario.Operation().IsRunning).IsTrue();
        await Assert.That(service.Calls).IsEqualTo(1);
        response.SetResult(AuthResult.Success("Neo"));
        OperationResult<AuthResult> completed = await execution.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(completed.Kind).IsEqualTo(OperationResultKind.Completed);
        await Assert.That(completed.Value).IsEqualTo(AuthResult.Success("Neo"));
        await Assert.That(scenario.Name()).IsEqualTo("new-edit");
        await Assert.That(scenario.Result()).IsEqualTo(AuthResult.Success("Neo"));
        await Assert.That(scenario.Operation().IsRunning).IsFalse();
    }

    /// <summary>业务失败、服务吞掉的协作取消与异常都有独立的操作结果。</summary>
    /// <param name="form">被验收的认证表单。</param>
    /// <param name="outcome">服务结束方式。</param>
    /// <returns>认证结果分类验证任务。</returns>
    [Test]
    [Arguments("Login", "BusinessFailure")]
    [Arguments("Register", "BusinessFailure")]
    [Arguments("ResetPassword", "BusinessFailure")]
    [Arguments("Login", "Canceled")]
    [Arguments("Register", "Canceled")]
    [Arguments("ResetPassword", "Canceled")]
    [Arguments("Login", "Faulted")]
    [Arguments("Register", "Faulted")]
    [Arguments("ResetPassword", "Faulted")]
    public async Task ServiceOutcomesRemainDistinctAsync(string form, string outcome)
    {
        using CancellationTokenSource cancellation = new();
        ControlledAuthService service = new()
        {
            Response = (_, _, _, _) =>
            {
                if (outcome == "Faulted") throw new InvalidOperationException("PRIVATE_PASSWORD_PAYLOAD");
                if (outcome == "Canceled") cancellation.Cancel();
                return Task.FromResult(AuthResult.Failure("服务器拒绝。"));
            }
        };
        AuthScenario scenario = CreateScenario(form, service);
        OperationResult<AuthResult> result = await scenario.Submit(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(10));
        OperationResultKind expected = outcome == "BusinessFailure" ? OperationResultKind.Completed
            : outcome == "Canceled" ? OperationResultKind.Canceled : OperationResultKind.Faulted;
        await Assert.That(result.Kind).IsEqualTo(expected);
        await Assert.That(scenario.Operation().LastResult).IsEqualTo(expected);
        await Assert.That(scenario.Operation().IsRunning).IsFalse();
        await Assert.That(scenario.Result()).IsEqualTo(outcome == "BusinessFailure" ? AuthResult.Failure("服务器拒绝。") : null);
    }

    /// <summary>注册规则按现有顺序报告错误，同时拒绝纯空白密码。</summary>
    /// <param name="name">用户名。</param>
    /// <param name="email">邮箱。</param>
    /// <param name="password">密码。</param>
    /// <param name="confirmation">确认密码。</param>
    /// <param name="expected">期望的可见错误。</param>
    /// <returns>纯规则验证任务。</returns>
    [Test]
    [Arguments("a", "invalid", "123", "456", "用户名至少需要 3 个字符。")]
    [Arguments("neo", "invalid", "123", "456", "邮箱格式不正确。")]
    [Arguments("neo", "neo@example.com", "123", "456", "密码长度至少为 6 位。")]
    [Arguments("neo", "neo@example.com", "abc123", "xyz123", "两次输入的密码不一致。")]
    [Arguments("neo", "neo@example.com", "      ", "      ", "请填写所有字段。")]
    public async Task RegisterRuleOrderAndBasicSubmissionConditionsAsync(string name, string email, string password, string confirmation, string expected)
    {
        ControlledAuthService service = new();
        RegisterFeature feature = new(service);
        feature.SetUserName(name);
        feature.SetEmail(email);
        feature.SetPassword(password);
        feature.SetConfirmPassword(confirmation);
        await Assert.That((await feature.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(feature.Snapshot.State.ValidationError).IsEqualTo(expected);
        await Assert.That(service.Calls).IsEqualTo(0);
    }

    private static AuthScenario CreateScenario(string form, IAuthService service)
    {
        if (form == "Login")
        {
            LoginFeature feature = new(service);
            feature.SetUserName("neo");
            feature.SetPassword("abc123");
            return new(feature.SubmitAsync, feature.SetUserName, () => feature.Snapshot.State.UserName,
                () => feature.Snapshot.State.Result, () => feature.Snapshot.OperationStates["SubmitAsync"]);
        }

        if (form == "Register")
        {
            RegisterFeature feature = new(service);
            feature.SetUserName("neo");
            feature.SetEmail("neo@example.com");
            feature.SetPassword("abc123");
            feature.SetConfirmPassword("abc123");
            return new(feature.SubmitAsync, feature.SetUserName, () => feature.Snapshot.State.UserName,
                () => feature.Snapshot.State.Result, () => feature.Snapshot.OperationStates["SubmitAsync"]);
        }

        ResetPasswordFeature reset = new(service);
        reset.SetUserName("neo");
        reset.SetNewPassword("abc123");
        reset.SetConfirmPassword("abc123");
        return new(reset.SubmitAsync, reset.SetUserName, () => reset.Snapshot.State.UserName,
            () => reset.Snapshot.State.Result, () => reset.Snapshot.OperationStates["SubmitAsync"]);
    }

    /// <summary>重置密码的错误顺序与非空白提交条件和旧版一致。</summary>
    /// <param name="name">用户名。</param>
    /// <param name="password">新密码。</param>
    /// <param name="confirmation">确认密码。</param>
    /// <param name="expected">期望错误。</param>
    /// <returns>重置密码规则验证任务。</returns>
    [Test]
    [Arguments("a", "123", "456", "用户名至少需要 3 个字符。")]
    [Arguments("neo", "123", "456", "新密码长度至少为 6 位。")]
    [Arguments("neo", "abc123", "xyz123", "两次输入的密码不一致。")]
    [Arguments("neo", "      ", "      ", "请填写所有字段。")]
    public async Task ResetPasswordRuleOrderAndBasicConditionsAsync(string name, string password, string confirmation, string expected)
    {
        ControlledAuthService service = new();
        ResetPasswordFeature feature = new(service);
        feature.SetUserName(name);
        feature.SetNewPassword(password);
        feature.SetConfirmPassword(confirmation);
        await Assert.That((await feature.SubmitAsync()).Kind).IsEqualTo(OperationResultKind.Rejected);
        await Assert.That(feature.Snapshot.State.ValidationError).IsEqualTo(expected);
        await Assert.That(service.Calls).IsEqualTo(0);
    }

    /// <summary>提供同一表单的公开执行与快照观察入口。</summary>
    /// <param name="Submit">生成的操作入口。</param>
    /// <param name="Edit">生成的编辑入口。</param>
    /// <param name="Name">当前已提交用户名。</param>
    /// <param name="Result">当前已提交业务结果。</param>
    /// <param name="Operation">当前已提交运行事实。</param>
    private sealed record AuthScenario(Func<CancellationToken, Task<OperationResult<AuthResult>>> Submit,
        Action<string> Edit, Func<string> Name, Func<AuthResult?> Result, Func<OperationState> Operation);
}

internal sealed class ControlledAuthService : IAuthService
{
    internal int Calls { get; private set; }
    internal Func<string, string, string?, CancellationToken, Task<AuthResult>> Response { get; set; }
        = static (name, _, _, _) => Task.FromResult(AuthResult.Success(name));

    /// <summary>执行可控登录服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">执行取消令牌。</param>
    /// <returns>可控认证结果。</returns>
    public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
        => Invoke(userName, password, null, cancellationToken);

    /// <summary>执行可控注册服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="email">邮箱。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">执行取消令牌。</param>
    /// <returns>可控认证结果。</returns>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
        => Invoke(userName, password, email, cancellationToken);

    /// <summary>执行可控重置密码服务。</summary>
    /// <param name="userName">用户名。</param>
    /// <param name="newPassword">新密码。</param>
    /// <param name="cancellationToken">执行取消令牌。</param>
    /// <returns>可控认证结果。</returns>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
        => Invoke(userName, newPassword, null, cancellationToken);

    private Task<AuthResult> Invoke(string name, string password, string? email, CancellationToken token)
    {
        Calls++;
        return Response(name, password, email, token);
    }
}
