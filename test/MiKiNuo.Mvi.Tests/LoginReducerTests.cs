using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示登录规约器测试。
/// </summary>
public sealed class LoginReducerTests
{
    /// <summary>
    /// 验证账号和密码非空时允许提交。
    /// </summary>
    [Test]
    public async Task ChangeUserNameAndPassword_Should_EnableSubmitAsync()
    {
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new NoopEffectDispatcher<LoginEffect>());

        await store.DispatchAsync(new LoginIntent.ChangeUserName("admin"));
        await store.DispatchAsync(new LoginIntent.ChangePassword("123456"));

        await Assert.That(store.CurrentState.CanSubmit).IsTrue();
    }

    /// <summary>
    /// 验证提交登录触发认证并产生导航副作用。
    /// </summary>
    [Test]
    public async Task Submit_Should_CallAuthServiceAndDispatchLoginSucceededAsync()
    {
        LoginState initialState = LoginState.Initial with
        {
            UserName = "admin",
            Password = "123456",
            CanSubmit = true
        };
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            initialState,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new NoopEffectDispatcher<LoginEffect>());
        List<LoginEffect> effects = [];
        IDisposable subscription = store.Effects.Subscribe(e => effects.Add(e));

        await store.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(effects.Count).IsEqualTo(1);
        await Assert.That(effects[0]).IsTypeOf<LoginEffect.NavigateToDashboard>();
        subscription.Dispose();
    }

    /// <summary>
    /// 验证登录失败设置错误消息并退出忙碌状态。
    /// </summary>
    [Test]
    public async Task LoginFailed_Should_SetErrorMessageAndExitBusyAsync()
    {
        LoginState initialState = LoginState.Initial with
        {
            UserName = "wrong",
            Password = "wrong",
            CanSubmit = true
        };
        using MviStore<LoginState, LoginIntent, LoginEffect> store = new(
            initialState,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginReducer(),
            new NoopEffectDispatcher<LoginEffect>());

        await store.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsNotNull();
    }
}
