using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页规约器。
/// </summary>
[MviFeature]
public sealed partial class ResetPasswordReducer
    : MviReducerBase<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect>
{
    /// <summary>
    /// 处理用户名变更意图。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.ChangeUserName))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleChangeUserName(
        ResetPasswordState state,
        ResetPasswordIntent.ChangeUserName intent)
    {
        return Unchanged(Recompute(state with { UserName = intent.UserName, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理新密码变更意图。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.ChangeNewPassword))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleChangeNewPassword(
        ResetPasswordState state,
        ResetPasswordIntent.ChangeNewPassword intent)
    {
        return Unchanged(Recompute(state with { NewPassword = intent.NewPassword, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理确认密码变更意图。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.ChangeConfirmPassword))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleChangeConfirmPassword(
        ResetPasswordState state,
        ResetPasswordIntent.ChangeConfirmPassword intent)
    {
        return Unchanged(Recompute(state with { ConfirmPassword = intent.ConfirmPassword, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理提交重置密码意图：声明联网重置副作用，表单随 Effect 快照。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleSubmit(
        ResetPasswordState state,
        ResetPasswordIntent.Submit intent)
    {
        return WithEffect(
            state with { IsBusy = true, ErrorMessage = null, CanSubmit = false },
            new ResetPasswordEffect.PerformResetPassword(state.UserName, state.NewPassword));
    }

    /// <summary>
    /// 处理重置成功回流意图：声明返回登录页副作用。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.Succeeded))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleSucceeded(
        ResetPasswordState state,
        ResetPasswordIntent.Succeeded intent)
    {
        return WithEffect(
            ResetPasswordState.Initial,
            new ResetPasswordEffect.ShowLoginPage());
    }

    /// <summary>
    /// 处理重置失败回流意图。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.Failed))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleFailed(
        ResetPasswordState state,
        ResetPasswordIntent.Failed intent)
    {
        return Unchanged(Recompute(state with
        {
            IsBusy = false,
            ErrorMessage = intent.ErrorMessage,
        }));
    }

    /// <summary>
    /// 处理返回登录页意图。
    /// </summary>
    [MviReduce(typeof(ResetPasswordIntent.GoLogin))]
    private MviReduceResult<ResetPasswordState, ResetPasswordEffect> HandleGoLogin(
        ResetPasswordState state,
        ResetPasswordIntent.GoLogin intent)
    {
        return WithEffect(state, new ResetPasswordEffect.ShowLoginPage());
    }

    private bool CanSubmitState(ResetPasswordState state) => state.CanSubmit;

    private ResetPasswordState Recompute(ResetPasswordState state)
    {
        return state with { CanSubmit = CanSubmit(state) };
    }

    private bool CanSubmit(ResetPasswordState state)
    {
        return !state.IsBusy
            && !string.IsNullOrWhiteSpace(state.UserName)
            && !string.IsNullOrWhiteSpace(state.NewPassword)
            && state.NewPassword == state.ConfirmPassword;
    }
}
