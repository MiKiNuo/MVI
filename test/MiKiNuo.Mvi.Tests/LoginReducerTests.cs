using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
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
        using MviMutationStore<LoginState, LoginIntent, LoginMutation, LoginEffect> store = new(
            LoginState.Initial,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginMutationReducer(),
            new EmptyLoginEffectDispatcher());

        await store.DispatchAsync(new LoginIntent.ChangeUserName("admin"));
        await store.DispatchAsync(new LoginIntent.ChangePassword("123456"));

        await Assert.That(store.CurrentState.CanSubmit).IsTrue();
    }

    /// <summary>
    /// 验证提交登录成功时产生导航副作用。
    /// </summary>
    [Test]
    public async Task Submit_Should_EmitNavigateToDashboardEffectAsync()
    {
        LoginState initialState = LoginState.Initial with
        {
            UserName = "admin",
            Password = "123456",
            CanSubmit = true
        };
        using MviMutationStore<LoginState, LoginIntent, LoginMutation, LoginEffect> store = new(
            initialState,
            new LoginIntentHandler(new FakeAuthService()),
            new LoginMutationReducer(),
            new EmptyLoginEffectDispatcher());
        List<LoginEffect> effects = [];
        IDisposable subscription = store.Effects.Subscribe(e => effects.Add(e));

        await store.DispatchAsync(new LoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(effects.Count).IsEqualTo(1);
        await Assert.That(effects[0]).IsTypeOf<LoginEffect.NavigateToDashboard>();
        subscription.Dispose();
    }

    private sealed class EmptyLoginEffectDispatcher
        : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<LoginEffect>
    {
        /// <summary>
        /// 分发副作用。
        /// </summary>
        /// <param name="effect">副作用。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步分发过程的任务。</returns>
        public ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
