using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页规约器。
/// </summary>
[MviFeature]
public sealed partial class RegisterReducer
    : MviReducerBase<RegisterState, RegisterIntent, RegisterEffect>
{
    /// <summary>
    /// 处理用户名变更意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.ChangeUserName))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleChangeUserName(
        RegisterState state,
        RegisterIntent.ChangeUserName intent)
    {
        return Unchanged(Recompute(state with { UserName = intent.UserName, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理邮箱变更意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.ChangeEmail))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleChangeEmail(
        RegisterState state,
        RegisterIntent.ChangeEmail intent)
    {
        return Unchanged(Recompute(state with { Email = intent.Email, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理密码变更意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.ChangePassword))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleChangePassword(
        RegisterState state,
        RegisterIntent.ChangePassword intent)
    {
        return Unchanged(Recompute(state with { Password = intent.Password, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理确认密码变更意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.ChangeConfirmPassword))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleChangeConfirmPassword(
        RegisterState state,
        RegisterIntent.ChangeConfirmPassword intent)
    {
        return Unchanged(Recompute(state with { ConfirmPassword = intent.ConfirmPassword, ErrorMessage = null }));
    }

    /// <summary>
    /// 处理提交注册意图：声明联网注册副作用，表单随 Effect 快照。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.Submit), Guard = nameof(CanSubmitState))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleSubmit(
        RegisterState state,
        RegisterIntent.Submit intent)
    {
        return WithEffect(
            state with { IsBusy = true, ErrorMessage = null, CanSubmit = false },
            new RegisterEffect.PerformRegister(state.UserName, state.Email, state.Password));
    }

    /// <summary>
    /// 处理注册成功回流意图：声明跳转登录页副作用。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.Succeeded))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleSucceeded(
        RegisterState state,
        RegisterIntent.Succeeded intent)
    {
        return WithEffect(
            RegisterState.Initial,
            new RegisterEffect.ShowLoginPage());
    }

    /// <summary>
    /// 处理注册失败回流意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.Failed))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleFailed(
        RegisterState state,
        RegisterIntent.Failed intent)
    {
        return Unchanged(Recompute(state with
        {
            IsBusy = false,
            ErrorMessage = intent.ErrorMessage,
        }));
    }

    /// <summary>
    /// 处理跳转到登录页意图。
    /// </summary>
    [MviReduce(typeof(RegisterIntent.GoLogin))]
    private MviReduceResult<RegisterState, RegisterEffect> HandleGoLogin(
        RegisterState state,
        RegisterIntent.GoLogin intent)
    {
        return WithEffect(state, new RegisterEffect.ShowLoginPage());
    }

    private bool CanSubmitState(RegisterState state) => state.CanSubmit;

    private RegisterState Recompute(RegisterState state)
    {
        return state with { CanSubmit = CanSubmit(state) };
    }

    private bool CanSubmit(RegisterState state)
    {
        return !state.IsBusy
            && !string.IsNullOrWhiteSpace(state.UserName)
            && !string.IsNullOrWhiteSpace(state.Email)
            && !string.IsNullOrWhiteSpace(state.Password);
    }
}
