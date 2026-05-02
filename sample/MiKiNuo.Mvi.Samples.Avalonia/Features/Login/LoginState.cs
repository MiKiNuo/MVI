using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面状态。
/// </summary>
/// <param name="UserName">用户账号。</param>
/// <param name="Password">用户密码。</param>
/// <param name="IsBusy">是否正在登录。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="CanSubmit">是否允许提交。</param>
public sealed record LoginState(
    string UserName,
    string Password,
    bool IsBusy,
    string? ErrorMessage,
    bool CanSubmit) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static LoginState Initial { get; } = new(
        string.Empty,
        string.Empty,
        false,
        null,
        false);
}
