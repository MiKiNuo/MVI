using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页状态。
/// </summary>
/// <param name="UserName">用户名。</param>
/// <param name="Email">邮箱。</param>
/// <param name="Password">密码。</param>
/// <param name="ConfirmPassword">确认密码。</param>
/// <param name="IsBusy">是否正在注册。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="CanSubmit">是否允许提交。</param>
public sealed record RegisterState(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword,
    bool IsBusy,
    string? ErrorMessage,
    bool CanSubmit) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static RegisterState Initial { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        false,
        null,
        false);
}
