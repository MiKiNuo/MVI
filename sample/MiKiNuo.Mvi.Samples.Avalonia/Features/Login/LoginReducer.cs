using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面规约器。
/// </summary>
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 处理用户名变更意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.ChangeUserName))]
    private MviReduceResult<LoginState, LoginEffect> HandleChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
            state with
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
        LoginIntent.ChangePassword intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
            state with
            {
                Password = intent.Password,
                ErrorMessage = null,
                CanSubmit = CanSubmit(state.UserName, intent.Password),
            });
    }

    /// <summary>
    /// 处理提交登录意图。
    /// </summary>
    [MviReduce(typeof(LoginIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<LoginState, LoginEffect> HandleSubmit(
        LoginState state,
        LoginIntent.Submit intent,
        IMviBusinessResult? result)
    {
        if (result is null)
        {
            return MviReduceResult.State<LoginState, LoginEffect>(
                state with { IsBusy = true, ErrorMessage = null });
        }

        if (result is LoginBusinessResult.Success success)
        {
            LoginState newState = state with { IsBusy = false, ErrorMessage = null, CanSubmit = true };
            return MviReduceResult.StateAndEffect<LoginState, LoginEffect>(
                newState,
                new LoginEffect.NavigateToDashboard(success.Profile.DisplayName));
        }

        if (result is LoginBusinessResult.Failure failure)
        {
            return MviReduceResult.State<LoginState, LoginEffect>(
                state with
                {
                    IsBusy = false,
                    ErrorMessage = failure.ErrorMessage,
                    CanSubmit = CanSubmit(state.UserName, state.Password),
                });
        }

        return MviReduceResult.State<LoginState, LoginEffect>(state);
    }

    private bool CanSubmitState(LoginState state) => state.CanSubmit || state.IsBusy;

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
