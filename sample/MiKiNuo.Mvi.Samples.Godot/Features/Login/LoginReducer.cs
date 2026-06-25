using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

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
        LoginIntent.ChangeUserName intent)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
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
        LoginIntent.ChangePassword intent)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
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
        LoginIntent.Submit intent)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
            state with { IsBusy = true, CanSubmit = false, LoginStatus = "正在登录..." });
    }

    /// <summary>处理登录成功意图。</summary>
    [MviReduce(typeof(LoginIntent.LoginSucceeded))]
    private MviReduceResult<LoginState, LoginEffect> HandleLoginSucceeded(
        LoginState state,
        LoginIntent.LoginSucceeded intent)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
            state with
            {
                IsBusy = false,
                ErrorMessage = null,
                LoginStatus = $"登录成功：{intent.Profile.PlayerName}，准备进入游戏大厅。",
            });
    }

    /// <summary>处理登录失败意图。</summary>
    [MviReduce(typeof(LoginIntent.LoginFailed))]
    private MviReduceResult<LoginState, LoginEffect> HandleLoginFailed(
        LoginState state,
        LoginIntent.LoginFailed intent)
    {
        return MviReduceResult.State<LoginState, LoginEffect>(
            state with
            {
                IsBusy = false,
                ErrorMessage = intent.ErrorMessage,
                CanSubmit = CanSubmit(state.UserName, state.Password),
                LoginStatus = "登录失败，请检查账号和密码。",
            });
    }

    private bool CanSubmitState(LoginState state) => CanSubmit(state.UserName, state.Password);

    private bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
