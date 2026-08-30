using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示联网登录注册示例的核心链路测试：
/// Reducer 决策断言、Guard 拦截、Effect 回流、Mediator 跨 Feature 导航、中间件全程可见。
/// </summary>
public sealed class SampleLoginFlowTests
{
    /// <summary>
    /// 验证 Submit 意图在满足 Guard 时声明 PerformLogin 副作用并快照凭证。
    /// </summary>
    [Test]
    public async Task Submit_Should_DeclarePerformLoginEffectWithCredentialSnapshotAsync()
    {
        LoginReducer reducer = new();
        LoginState state = LoginState.Initial with { UserName = "emilys", Password = "emilyspass", CanSubmit = true };

        MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> result =
            reducer.Reduce(state, new LoginIntent.Submit());

        await Assert.That(result.State.IsBusy).IsTrue();
        await Assert.That(result.Effects.Count).IsEqualTo(1);
        LoginEffect.PerformLogin effect = (LoginEffect.PerformLogin)result.Effects[0];
        await Assert.That(effect.UserName).IsEqualTo("emilys");
        await Assert.That(effect.Password).IsEqualTo("emilyspass");
    }

    /// <summary>
    /// 验证 CanSubmit 为 false 时 Guard 拦截 Submit，状态不变且不产生副作用。
    /// </summary>
    [Test]
    public async Task Submit_WhenCannotSubmit_Should_BeBlockedByGuardAsync()
    {
        LoginReducer reducer = new();

        MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<LoginState, LoginEffect> result =
            reducer.Reduce(LoginState.Initial, new LoginIntent.Submit());

        await Assert.That(result.State).IsEqualTo(LoginState.Initial);
        await Assert.That(result.Effects.Count).IsEqualTo(0);
    }

    /// <summary>
    /// 验证注册页 CanSubmit 只要求字段填齐（业务规则校验由中间件承载）。
    /// </summary>
    [Test]
    public async Task Register_CanSubmit_Should_RequireAllFieldsFilledAsync()
    {
        RegisterReducer reducer = new();
        RegisterState state = RegisterState.Initial;

        state = reducer.Reduce(state, new RegisterIntent.ChangeUserName("neo")).State;
        state = reducer.Reduce(state, new RegisterIntent.ChangeEmail("neo@example.com")).State;
        await Assert.That(state.CanSubmit).IsFalse();

        state = reducer.Reduce(state, new RegisterIntent.ChangePassword("abc123")).State;
        await Assert.That(state.CanSubmit).IsTrue();
    }

    /// <summary>
    /// 验证完整登录链路：输入 → 提交 → 联网(伪) → 回流成功 → Mediator 导航主页；
    /// 且回流意图对中间件可见（追踪链完整）。
    /// </summary>
    [Test]
    public async Task LoginFlow_Should_CompleteEndToEndWithMediatorNavigationAsync()
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
                ShellPage.Home => new AppShellIntent.ShowHome(request.DisplayName ?? string.Empty),
                ShellPage.Register => new AppShellIntent.ShowRegister(),
                _ => new AppShellIntent.ShowLogin(),
            };
            await shellStore.DispatchAsync(intent, cancellationToken);
            return true;
        });

        LoginAuditMiddleware auditMiddleware = new();
        LoginEffectDispatcher effectDispatcher = new(new FakeSuccessAuthService(), mediator);
        using MviStore<LoginState, LoginIntent, LoginEffect> loginStore = new(
            LoginState.Initial,
            new LoginReducer(),
            effectDispatcher,
            [auditMiddleware]);

        await loginStore.DispatchAsync(new LoginIntent.ChangeUserName("emilys"));
        await loginStore.DispatchAsync(new LoginIntent.ChangePassword("emilyspass"));
        await loginStore.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(shellStore.CurrentState.CurrentPage).IsEqualTo(ShellPage.Home);
        await Assert.That(shellStore.CurrentState.DisplayName).IsEqualTo("Emily Johnson");
        await Assert.That(loginStore.CurrentState.IsBusy).IsFalse();

        // 回流意图 Succeeded 必须出现在中间件追踪链中。
        await Assert.That(auditMiddleware.Trail).Contains(nameof(LoginIntent.Submit));
        await Assert.That(auditMiddleware.Trail).Contains(nameof(LoginIntent.Succeeded));
    }

    /// <summary>
    /// 验证登录失败链路：回流失败意图并呈现错误消息。
    /// </summary>
    [Test]
    public async Task LoginFlow_WhenAuthFails_Should_SurfaceErrorMessageAsync()
    {
        MviMediator mediator = new();
        LoginEffectDispatcher effectDispatcher = new(new FakeFailureAuthService(), mediator);
        using MviStore<LoginState, LoginIntent, LoginEffect> loginStore = new(
            LoginState.Initial,
            new LoginReducer(),
            effectDispatcher);

        await loginStore.DispatchAsync(new LoginIntent.ChangeUserName("emilys"));
        await loginStore.DispatchAsync(new LoginIntent.ChangePassword("wrong"));
        await loginStore.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(loginStore.CurrentState.IsBusy).IsFalse();
        await Assert.That(loginStore.CurrentState.ErrorMessage).IsEqualTo("凭据无效");
        await Assert.That(loginStore.CurrentState.CanSubmit).IsTrue();
    }

    private sealed class FakeSuccessAuthService : IAuthService
    {
        /// <inheritdoc />
        public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Success("Emily Johnson"));
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

    private sealed class FakeFailureAuthService : IAuthService
    {
        /// <inheritdoc />
        public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Failure("凭据无效"));
        }

        /// <inheritdoc />
        public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Failure("注册失败"));
        }

        /// <inheritdoc />
        public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken)
        {
            return Task.FromResult(AuthResult.Failure("重置密码失败"));
        }
    }
}
