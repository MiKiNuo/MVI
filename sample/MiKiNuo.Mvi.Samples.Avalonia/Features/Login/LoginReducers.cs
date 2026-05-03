using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录状态规约器。
/// </summary>
public sealed partial class LoginReducer
    : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 处理修改账号意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.ChangeUserName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LoginState nextState = state with
        {
            UserName = intent.UserName,
            CanSubmit = CanSubmit(intent.UserName, state.Password, state.IsBusy)
        };

        return MviReduceResult.State<LoginState, LoginEffect>(nextState);
    }

    /// <summary>
    /// 处理修改密码意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.ChangePassword intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LoginState nextState = state with
        {
            Password = intent.Password,
            CanSubmit = CanSubmit(state.UserName, intent.Password, state.IsBusy)
        };

        return MviReduceResult.State<LoginState, LoginEffect>(nextState);
    }

    /// <summary>
    /// 处理提交登录意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.Submit intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (!state.CanSubmit)
        {
            return MviReduceResult.State<LoginState, LoginEffect>(state);
        }

        LoginState nextState = state with
        {
            IsBusy = true,
            ErrorMessage = null,
            CanSubmit = false
        };

        return MviReduceResult.StateAndEffect<LoginState, LoginEffect>(
            nextState,
            new LoginEffect.RequestLogin(state.UserName, state.Password));
    }

    /// <summary>
    /// 处理登录成功意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.LoginSucceeded intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LoginState nextState = state with
        {
            IsBusy = false,
            ErrorMessage = null,
            CanSubmit = true
        };

        return MviReduceResult.StateAndEffect<LoginState, LoginEffect>(
            nextState,
            new LoginEffect.NavigateToDashboard(intent.DisplayName));
    }

    /// <summary>
    /// 处理登录失败意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(
        LoginState state,
        LoginIntent.LoginFailed intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        LoginState nextState = state with
        {
            IsBusy = false,
            ErrorMessage = intent.ErrorMessage,
            CanSubmit = CanSubmit(state.UserName, state.Password, false)
        };

        return MviReduceResult.State<LoginState, LoginEffect>(nextState);
    }

    private static bool CanSubmit(string userName, string password, bool isBusy)
    {
        return !isBusy
            && !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
