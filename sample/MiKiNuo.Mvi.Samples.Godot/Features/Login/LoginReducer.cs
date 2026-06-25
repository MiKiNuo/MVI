using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录规约器。
/// </summary>
public sealed class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            LoginIntent.ChangeUserName changeUserName => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    UserName = changeUserName.UserName,
                    ErrorMessage = null,
                    CanSubmit = CanSubmit(changeUserName.UserName, state.Password),
                    LoginStatus = "账号已更新，登录按钮状态由 MviCommand CanExecute 自动刷新。",
                }),
            LoginIntent.ChangePassword changePassword => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    Password = changePassword.Password,
                    ErrorMessage = null,
                    CanSubmit = CanSubmit(state.UserName, changePassword.Password),
                    LoginStatus = "密码已更新，ViewModel 双向绑定会生成 ChangePassword Intent。",
                }),
            LoginIntent.Submit when CanSubmit(state.UserName, state.Password) => MviReduceResult.State<LoginState, LoginEffect>(
                state with { IsBusy = true, CanSubmit = false, LoginStatus = "正在登录..." }),
            LoginIntent.LoginSucceeded succeeded => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    IsBusy = false,
                    ErrorMessage = null,
                    LoginStatus = $"登录成功：{succeeded.Profile.PlayerName}，准备进入游戏大厅。",
                }),
            LoginIntent.LoginFailed failed => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    IsBusy = false,
                    ErrorMessage = failed.ErrorMessage,
                    CanSubmit = CanSubmit(state.UserName, state.Password),
                    LoginStatus = "登录失败，请检查账号和密码。",
                }),
            _ => MviReduceResult.State<LoginState, LoginEffect>(state),
        };
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
