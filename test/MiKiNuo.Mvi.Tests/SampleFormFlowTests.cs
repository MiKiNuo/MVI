using MiKiNuo.Mvi.Abstractions.MVI.Mediator;
using MiKiNuo.Mvi.Runtime.MVI.Mediator;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>注册和重置使用同一套简洁声明 API。</summary>
public sealed class SampleFormFlowTests
{
    /// <summary>忘记密码直接进入重置演示，不需要恢复码申请页或本地服务器。</summary>
    [Test] public async Task ForgotPasswordOpensResetDemoDirectlyAsync()
    {
        ProbeAuthApi api = new(); ProbeMediator mediator = new();
        await using var store = new MviStore<LoginState, LoginIntent>(LoginState.Initial, new LoginHandler(api, mediator), new LoginReducer());
        using LoginViewModel model = new(store);
        await model.GoResetPasswordCommand.ExecuteAsync(null);
        await Assert.That(mediator.Requests.Single().Page).IsEqualTo(ShellPage.ResetPassword);
        await Assert.That(api.FormCalls).IsEqualTo(0);
        await Assert.That(model.State.IsBusy).IsFalse();
    }
    /// <summary>不一致的密码不会访问服务。</summary>
    [Test] public async Task RegisterMismatchStopsBeforeServiceAsync()
    {
        ProbeAuthApi api = new();
        await using var store = new MviStore<RegisterState, RegisterIntent>(RegisterState.Initial, new RegisterHandler(api, new ProbeMediator()), new RegisterReducer());
        using RegisterViewModel model = new(store) { UserName = "alice", Email = "a@example.test", Password = "one", ConfirmPassword = "two" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(api.FormCalls).IsEqualTo(0);
        await Assert.That(model.State.ErrorMessage is not null).IsTrue();
        await Assert.That(model.Password).IsEqualTo("");
        await Assert.That(model.ConfirmPassword).IsEqualTo("");
    }
    /// <summary>注册成功返回登录页。</summary>
    [Test] public async Task RegistrationNavigatesToLoginAsync()
    {
        ProbeAuthApi api = new(); ProbeMediator mediator = new();
        await using var store = new MviStore<RegisterState, RegisterIntent>(RegisterState.Initial, new RegisterHandler(api, mediator), new RegisterReducer());
        using RegisterViewModel model = new(store) { UserName = "alice", Email = "a@example.test", Password = "password", ConfirmPassword = "password" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(mediator.Requests.Single().Page).IsEqualTo(ShellPage.Login);
        await Assert.That(model.State.IsBusy).IsFalse();
    }
    /// <summary>重置密码的多个敏感输入在快照后清空。</summary>
    [Test] public async Task ResetClearsSensitiveDraftsAndReturnsToLoginAsync()
    {
        ProbeMediator mediator = new();
        await using var store = new MviStore<ResetPasswordState, ResetPasswordIntent>(ResetPasswordState.Initial, new ResetPasswordHandler(new ProbeAuthApi(), mediator), new ResetPasswordReducer());
        using ResetPasswordViewModel model = new(store) { UserName = "alice", NewPassword = "password", ConfirmPassword = "password" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(model.NewPassword + model.ConfirmPassword).IsEqualTo("");
        await Assert.That(mediator.Requests.Single().Page).IsEqualTo(ShellPage.Login);
    }
}
