using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Tests.TestSupport;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示注册/重置密码表单校验中间件测试：
/// 校验失败时阻断规约与副作用并把错误消息写回状态，校验通过时正常进入 Reducer。
/// </summary>
public sealed class SampleValidationMiddlewareTests
{
    /// <summary>
    /// 验证邮箱格式错误时阻断注册提交并写回错误消息。
    /// </summary>
    [Test]
    public async Task Register_SubmitWithInvalidEmail_Should_BeBlockedWithErrorAsync()
    {
        using MviStore<RegisterState, RegisterIntent, RegisterEffect> store = CreateRegisterStore();

        await store.DispatchAsync(new RegisterIntent.ChangeUserName("neo"));
        await store.DispatchAsync(new RegisterIntent.ChangeEmail("not-an-email"));
        await store.DispatchAsync(new RegisterIntent.ChangePassword("abc123"));
        await store.DispatchAsync(new RegisterIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsEqualTo("邮箱格式不正确。");
    }

    /// <summary>
    /// 验证两次密码不一致时阻断注册提交并写回错误消息。
    /// </summary>
    [Test]
    public async Task Register_SubmitWithMismatchedPassword_Should_BeBlockedWithErrorAsync()
    {
        using MviStore<RegisterState, RegisterIntent, RegisterEffect> store = CreateRegisterStore();

        await store.DispatchAsync(new RegisterIntent.ChangeUserName("neo"));
        await store.DispatchAsync(new RegisterIntent.ChangeEmail("neo@example.com"));
        await store.DispatchAsync(new RegisterIntent.ChangePassword("abc123"));
        await store.DispatchAsync(new RegisterIntent.ChangeConfirmPassword("different"));
        await store.DispatchAsync(new RegisterIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsEqualTo("两次输入的密码不一致。");
    }

    /// <summary>
    /// 验证校验通过时注册提交正常进入 Reducer 并声明副作用。
    /// </summary>
    [Test]
    public async Task Register_SubmitWithValidInput_Should_PassThroughToReducerAsync()
    {
        using MviStore<RegisterState, RegisterIntent, RegisterEffect> store = CreateRegisterStore();

        await store.DispatchAsync(new RegisterIntent.ChangeUserName("neo"));
        await store.DispatchAsync(new RegisterIntent.ChangeEmail("neo@example.com"));
        await store.DispatchAsync(new RegisterIntent.ChangePassword("abc123"));
        await store.DispatchAsync(new RegisterIntent.ChangeConfirmPassword("abc123"));
        await store.DispatchAsync(new RegisterIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsTrue();
        await Assert.That(store.CurrentState.ErrorMessage).IsNull();
    }

    /// <summary>
    /// 验证新密码过短时阻断重置提交并写回错误消息。
    /// </summary>
    [Test]
    public async Task ResetPassword_SubmitWithShortPassword_Should_BeBlockedWithErrorAsync()
    {
        using MviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> store = CreateResetPasswordStore();

        await store.DispatchAsync(new ResetPasswordIntent.ChangeUserName("emilys"));
        await store.DispatchAsync(new ResetPasswordIntent.ChangeNewPassword("abc"));
        await store.DispatchAsync(new ResetPasswordIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsEqualTo("新密码长度至少为 6 位。");
    }

    /// <summary>
    /// 验证校验通过时重置提交正常进入 Reducer。
    /// </summary>
    [Test]
    public async Task ResetPassword_SubmitWithValidInput_Should_PassThroughToReducerAsync()
    {
        using MviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> store = CreateResetPasswordStore();

        await store.DispatchAsync(new ResetPasswordIntent.ChangeUserName("emilys"));
        await store.DispatchAsync(new ResetPasswordIntent.ChangeNewPassword("newpass"));
        await store.DispatchAsync(new ResetPasswordIntent.ChangeConfirmPassword("newpass"));
        await store.DispatchAsync(new ResetPasswordIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsTrue();
        await Assert.That(store.CurrentState.ErrorMessage).IsNull();
    }

    private static MviStore<RegisterState, RegisterIntent, RegisterEffect> CreateRegisterStore()
    {
        return new MviStore<RegisterState, RegisterIntent, RegisterEffect>(
            RegisterState.Initial,
            new RegisterReducer(),
            new NoopEffectDispatcher<RegisterEffect>(),
            [new RegisterValidationMiddleware()]);
    }

    private static MviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> CreateResetPasswordStore()
    {
        return new MviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect>(
            ResetPasswordState.Initial,
            new ResetPasswordReducer(),
            new NoopEffectDispatcher<ResetPasswordEffect>(),
            [new ResetPasswordValidationMiddleware()]);
    }
}
