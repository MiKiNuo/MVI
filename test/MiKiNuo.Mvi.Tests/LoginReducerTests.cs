using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
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
        LoginState state = LoginState.Initial;
        state = new LoginReducer().Reduce(state, new LoginIntent.ChangeUserName("admin")).State;
        state = new LoginReducer().Reduce(state, new LoginIntent.ChangePassword("123456")).State;

        await Assert.That(state.CanSubmit).IsTrue();
    }

    /// <summary>
    /// 验证提交登录时产生请求登录副作用。
    /// </summary>
    [Test]
    public async Task Submit_Should_EmitRequestLoginEffectAsync()
    {
        LoginState state = LoginState.Initial with
        {
            UserName = "admin",
            Password = "123456",
            CanSubmit = true
        };

        MviReduceResult<LoginState, LoginEffect> result = new LoginReducer().Reduce(state, new LoginIntent.Submit());

        await Assert.That(result.State.IsBusy).IsTrue();
        await Assert.That(result.Effects.Count).IsEqualTo(1);
        await Assert.That(result.Effects[0]).IsTypeOf<LoginEffect.RequestLogin>();
    }
}
