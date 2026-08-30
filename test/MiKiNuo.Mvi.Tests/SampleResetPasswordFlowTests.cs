using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示忘记密码链路测试：登录页跳转重置密码页、提交重置、成功后返回登录页。
/// </summary>
public sealed class SampleResetPasswordFlowTests
{
    /// <summary>
    /// 验证登录页 GoResetPassword 意图声明跳转重置密码页副作用。
    /// </summary>
    [Test]
    public async Task GoResetPassword_Should_DeclareShowResetPasswordPageEffectAsync()
    {
        LoginReducer reducer = new();

        MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> result =
            reducer.Reduce(LoginState.Initial, new LoginIntent.GoResetPassword());

        await Assert.That(result.Effects.Count).IsEqualTo(1);
        await Assert.That(result.Effects[0]).IsTypeOf<LoginEffect.ShowResetPasswordPage>();
    }

    /// <summary>
    /// 验证重置密码 CanSubmit 只要求字段填齐（业务规则校验由中间件承载）。
    /// </summary>
    [Test]
    public async Task ResetPassword_CanSubmit_Should_RequireAllFieldsFilledAsync()
    {
        ResetPasswordReducer reducer = new();
        ResetPasswordState state = ResetPasswordState.Initial;

        state = reducer.Reduce(state, new ResetPasswordIntent.ChangeUserName("emilys")).State;
        await Assert.That(state.CanSubmit).IsFalse();

        state = reducer.Reduce(state, new ResetPasswordIntent.ChangeNewPassword("newpass")).State;
        await Assert.That(state.CanSubmit).IsTrue();
    }

    /// <summary>
    /// 验证完整链路：登录页 → 重置密码页 → 提交 → 联网(伪)重置成功 → 返回登录页。
    /// </summary>
    [Test]
    public async Task ResetPasswordFlow_Should_CompleteEndToEndAsync()
    {
        MviMediator mediator = new();
        using MviStore<AppShellState, AppShellIntent, UnitEffect> shellStore = new(
            AppShellState.Initial,
            new AppShellReducer(),
            NullEffectDispatcher.Instance);
        mediator.Register<NavigateToPageRequest, bool>(async (request, cancellationToken) =>
        {
            AppShellIntent intent = request.Page switch
            {
                ShellPage.ResetPassword => new AppShellIntent.ShowResetPassword(),
                ShellPage.Home => new AppShellIntent.ShowHome(request.DisplayName ?? string.Empty),
                ShellPage.Register => new AppShellIntent.ShowRegister(),
                _ => new AppShellIntent.ShowLogin(),
            };
            await shellStore.DispatchAsync(intent, cancellationToken);
            return true;
        });

        ResetPasswordEffectDispatcher resetDispatcher = new(new FakeResetAuthService(), mediator);
        using MviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> resetStore = new(
            ResetPasswordState.Initial,
            new ResetPasswordReducer(),
            resetDispatcher);

        // 登录页跳转到重置密码页（经 Mediator 协调应用壳）。
        LoginEffectDispatcher loginDispatcher = new(new FakeResetAuthService(), mediator);
        using MviStore<LoginState, LoginIntent, LoginEffect> loginStore = new(
            LoginState.Initial,
            new LoginReducer(),
            loginDispatcher);
        await loginStore.DispatchAsync(new LoginIntent.GoResetPassword());
        await Assert.That(shellStore.CurrentState.CurrentPage).IsEqualTo(ShellPage.ResetPassword);

        // 填写表单并提交重置。
        await resetStore.DispatchAsync(new ResetPasswordIntent.ChangeUserName("emilys"));
        await resetStore.DispatchAsync(new ResetPasswordIntent.ChangeNewPassword("newpass"));
        await resetStore.DispatchAsync(new ResetPasswordIntent.ChangeConfirmPassword("newpass"));
        await resetStore.DispatchAsync(new ResetPasswordIntent.Submit());

        // 重置成功后返回登录页，表单重置为初始状态。
        await Assert.That(shellStore.CurrentState.CurrentPage).IsEqualTo(ShellPage.Login);
        await Assert.That(resetStore.CurrentState).IsEqualTo(ResetPasswordState.Initial);
    }

    private sealed class FakeResetAuthService : IAuthService
    {
        /// <inheritdoc />
        public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Success(userName));
        }

        /// <inheritdoc />
        public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Success(userName));
        }

        /// <inheritdoc />
        public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Success(userName));
        }
    }
}
