using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录 Reducer。
/// </summary>
public sealed partial class LoginReducer : MviReducerBase<LoginState, LoginIntent, LoginEffect>
{
    private readonly GameLogicService _gameLogicService;

    /// <summary>
    /// 初始化游戏登录 Reducer。
    /// </summary>
    /// <param name="gameLogicService">共享游戏逻辑服务。</param>
    public LoginReducer(GameLogicService gameLogicService)
    {
        _gameLogicService = gameLogicService ?? throw new ArgumentNullException(nameof(gameLogicService));
    }

    /// <summary>
    /// 处理修改账号意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">修改账号意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(LoginState state, LoginIntent.ChangeUserName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        LoginState nextState = state with
        {
            UserName = intent.UserName,
            ErrorMessage = null,
            CanSubmit = CanSubmit(intent.UserName, state.Password),
            LoginStatus = "账号已更新，登录按钮状态由 MviCommand CanExecute 自动刷新。",
        };
        return MviReduceResult.State<LoginState, LoginEffect>(nextState);
    }

    /// <summary>
    /// 处理修改密码意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">修改密码意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(LoginState state, LoginIntent.ChangePassword intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        LoginState nextState = state with
        {
            Password = intent.Password,
            ErrorMessage = null,
            CanSubmit = CanSubmit(state.UserName, intent.Password),
            LoginStatus = "密码已更新，ViewModel 双向绑定会生成 ChangePassword Intent。",
        };
        return MviReduceResult.State<LoginState, LoginEffect>(nextState);
    }

    /// <summary>
    /// 处理登录提交意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">登录提交意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<LoginState, LoginEffect> Reduce(LoginState state, LoginIntent.Submit intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (!CanSubmit(state.UserName, state.Password))
        {
            LoginState failedState = state with
            {
                ErrorMessage = "账号不能为空，密码长度至少 3 位。",
                CanSubmit = false,
                LoginStatus = "登录校验失败，状态由 Reducer 返回。",
            };
            return MviReduceResult.StateAndEffect<LoginState, LoginEffect>(
                failedState,
                new LoginEffect.Trace("Login validation failed"));
        }

        PlayerProfile profile = _gameLogicService.CreateProfile(state.UserName);
        LoginState successState = state with
        {
            IsBusy = false,
            ErrorMessage = null,
            LoginStatus = $"登录成功：{profile.PlayerName}，准备进入游戏大厅。",
        };
        return MviReduceResult.StateAndEffects<LoginState, LoginEffect>(
            successState,
            [new LoginEffect.Trace($"Login succeeded for {profile.PlayerName}"), new LoginEffect.LoginSucceeded(profile)]);
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password) && password.Length >= 3;
    }
}
