using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页规约器。
/// </summary>
[MviFeature]
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 处理用户名变更意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.ChangeUserName))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent)
    {
        return Unchanged(state with
        {
            UserName = intent.UserName,
            ErrorMessage = null,
            CanSubmit = CanSubmit(intent.UserName, state.Password),
        });
    }

    /// <summary>
    /// 处理密码变更意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.ChangePassword))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangePassword(
        LoginState state,
        LoginIntent.ChangePassword intent)
    {
        return Unchanged(state with
        {
            Password = intent.Password,
            ErrorMessage = null,
            CanSubmit = CanSubmit(state.UserName, intent.Password),
        });
    }

    /// <summary>
    /// 处理提交登录意图：声明联网登录副作用，凭证随 Effect 快照。
    /// </summary>
    [MviReduce(typeof(LoginIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<LoginState, LoginEffect> HandleSubmit(
        LoginState state,
        LoginIntent.Submit intent)
    {
        return WithEffect(
            state with { IsBusy = true, ErrorMessage = null, CanSubmit = false },
            new LoginEffect.PerformLogin(state.UserName, state.Password));
    }

    /// <summary>
    /// 处理登录成功回流意图：声明跳转主页副作用。
    /// </summary>
    [MviReduce(typeof(LoginIntent.Succeeded))]
    private MviReduceResult<LoginState, LoginEffect> HandleSucceeded(
        LoginState state,
        LoginIntent.Succeeded intent)
    {
        return WithEffect(
            state with { IsBusy = false, ErrorMessage = null },
            new LoginEffect.ShowHomePage(intent.DisplayName));
    }

    /// <summary>
    /// 处理登录失败回流意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.Failed))]
    private MviReduceResult<LoginState, LoginEffect> HandleFailed(
        LoginState state,
        LoginIntent.Failed intent)
    {
        return Unchanged(state with
        {
            IsBusy = false,
            ErrorMessage = intent.ErrorMessage,
            CanSubmit = CanSubmit(state.UserName, state.Password),
        });
    }

    /// <summary>
    /// 处理跳转到注册页意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.GoRegister))]
    private MviReduceResult<LoginState, LoginEffect> HandleGoRegister(
        LoginState state,
        LoginIntent.GoRegister intent)
    {
        return WithEffect(state, new LoginEffect.ShowRegisterPage());
    }

    /// <summary>
    /// 处理跳转到重置密码页意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.GoResetPassword))]
    private MviReduceResult<LoginState, LoginEffect> HandleGoResetPassword(
        LoginState state,
        LoginIntent.GoResetPassword intent)
    {
        return WithEffect(state, new LoginEffect.ShowResetPasswordPage());
    }

    private bool CanSubmitState(LoginState state) => state.CanSubmit;

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
