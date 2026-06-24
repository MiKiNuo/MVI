using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录变更规约器。
/// </summary>
public sealed partial class LoginMutationReducer
    : MviMutationReducerBase<LoginState, LoginMutation, LoginEffect>
{
    /// <summary>
    /// 应用设置用户账号变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetUserName(
        LoginState state,
        LoginMutation.SetUserName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置用户密码变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetPassword(
        LoginState state,
        LoginMutation.SetPassword mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置登录中状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetIsBusy(
        LoginState state,
        LoginMutation.SetIsBusy mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置错误消息变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetErrorMessage(
        LoginState state,
        LoginMutation.SetErrorMessage mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可提交状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetCanSubmit(
        LoginState state,
        LoginMutation.SetCanSubmit mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置登录状态说明变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LoginState, LoginEffect> HandleSetLoginStatus(
        LoginState state,
        LoginMutation.SetLoginStatus mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LoginState, LoginEffect>(state.Apply(mutation));
    }
}
