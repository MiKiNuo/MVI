using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面规约器。
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
                }),
            LoginIntent.ChangePassword changePassword => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    Password = changePassword.Password,
                    ErrorMessage = null,
                    CanSubmit = CanSubmit(state.UserName, changePassword.Password),
                }),
            LoginIntent.Submit when state.CanSubmit => MviReduceResult.State<LoginState, LoginEffect>(
                state with { IsBusy = true, ErrorMessage = null, CanSubmit = false }),
            LoginIntent.LoginSucceeded => MviReduceResult.State<LoginState, LoginEffect>(
                state with { IsBusy = false, ErrorMessage = null, CanSubmit = true }),
            LoginIntent.LoginFailed failed => MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    IsBusy = false,
                    ErrorMessage = failed.ErrorMessage,
                    CanSubmit = CanSubmit(state.UserName, state.Password),
                }),
            _ => MviReduceResult.State<LoginState, LoginEffect>(state),
        };
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
