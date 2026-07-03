using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录规约器。
/// </summary>
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>处理账号变更意图。</summary>
    [MviReduce(typeof(LoginIntent.ChangeUserName))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent,
        IMviBusinessResult? result)
    {
        return Unchanged(
            state with
            {
                UserName = intent.UserName,
                ErrorMessage = null,
                CanSubmit = CanSubmit(intent.UserName, state.Password),
                LoginStatus = "账号已更新，登录按钮状态由 MviCommand CanExecute 自动刷新。",
            });
    }

    /// <summary>处理密码变更意图。</summary>
    [MviReduce(typeof(LoginIntent.ChangePassword))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangePassword(
        LoginState state,
        LoginIntent.ChangePassword intent,
        IMviBusinessResult? result)
    {
        return Unchanged(
            state with
            {
                Password = intent.Password,
                ErrorMessage = null,
                CanSubmit = CanSubmit(state.UserName, intent.Password),
                LoginStatus = "密码已更新，ViewModel 双向绑定会生成 ChangePassword Intent。",
            });
    }

    /// <summary>处理提交登录意图。</summary>
    [MviReduce(typeof(LoginIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<LoginState, LoginEffect> HandleSubmit(
        LoginState state,
        LoginIntent.Submit intent,
        IMviBusinessResult? result)
    {
        if (result is null)
        {
            return Unchanged(
                state with { IsBusy = true, LoginStatus = "正在登录..." });
        }

        if (result is LoginBusinessResult.Success success)
        {
            PlayerProfile profile = (PlayerProfile)success.Profile;
            LoginState newState = state with
            {
                IsBusy = false,
                ErrorMessage = null,
                LoginStatus = $"登录成功：{profile.PlayerName}，准备进入游戏大厅。",
            };
            return WithEffects(
                newState,
                new LoginEffect[]
                {
                    new LoginEffect.LoginSucceeded(profile),
                    new LoginEffect.Trace($"Login succeeded for {profile.PlayerName}"),
                });
        }

        if (result is LoginBusinessResult.Failure failure)
        {
            LoginState newState = state with
            {
                IsBusy = false,
                ErrorMessage = failure.ErrorMessage,
                CanSubmit = CanSubmit(state.UserName, state.Password),
                LoginStatus = "登录失败，请检查账号和密码。",
            };
            return WithEffect(
                newState,
                new LoginEffect.Trace("Login validation failed"));
        }

        return Unchanged(state);
    }

    private bool CanSubmitState(LoginState state) => CanSubmit(state.UserName, state.Password) || state.IsBusy;

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
