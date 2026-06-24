using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录 MVI 变更。
/// </summary>
public abstract record LoginMutation : IMviMutation<LoginState>
{
    /// <summary>
    /// 表示设置用户账号的变更。
    /// </summary>
    /// <param name="Value">用户账号。</param>
    [MviMutation(Path = "UserName")]
    public sealed record SetUserName(string Value) : LoginMutation;

    /// <summary>
    /// 表示设置用户密码的变更。
    /// </summary>
    /// <param name="Value">用户密码。</param>
    [MviMutation(Path = "Password")]
    public sealed record SetPassword(string Value) : LoginMutation;

    /// <summary>
    /// 表示设置登录中状态的变更。
    /// </summary>
    /// <param name="Value">是否正在登录。</param>
    [MviMutation(Path = "IsBusy")]
    public sealed record SetIsBusy(bool Value) : LoginMutation;

    /// <summary>
    /// 表示设置错误消息的变更。
    /// </summary>
    /// <param name="Value">错误消息。</param>
    [MviMutation(Path = "ErrorMessage")]
    public sealed record SetErrorMessage(string? Value) : LoginMutation;

    /// <summary>
    /// 表示设置可提交状态的变更。
    /// </summary>
    /// <param name="Value">是否允许提交。</param>
    [MviMutation(Path = "CanSubmit")]
    public sealed record SetCanSubmit(bool Value) : LoginMutation;

    /// <summary>
    /// 表示设置登录状态说明的变更。
    /// </summary>
    /// <param name="Value">登录状态说明。</param>
    [MviMutation(Path = "LoginStatus")]
    public sealed record SetLoginStatus(string Value) : LoginMutation;
}
