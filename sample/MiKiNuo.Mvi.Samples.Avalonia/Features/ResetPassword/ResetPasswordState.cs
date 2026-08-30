using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页状态。
/// </summary>
/// <param name="UserName">用户名。</param>
/// <param name="NewPassword">新密码。</param>
/// <param name="ConfirmPassword">确认新密码。</param>
/// <param name="IsBusy">是否正在提交。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="CanSubmit">是否允许提交。</param>
public sealed record ResetPasswordState(
    string UserName,
    string NewPassword,
    string ConfirmPassword,
    bool IsBusy,
    string? ErrorMessage,
    bool CanSubmit) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static ResetPasswordState Initial { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        false,
        null,
        false);
}
