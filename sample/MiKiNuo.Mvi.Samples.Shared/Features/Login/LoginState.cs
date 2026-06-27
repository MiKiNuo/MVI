using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Shared.Features.Login;

/// <summary>
/// 表示登录界面状态。
/// </summary>
/// <param name="UserName">用户账号。</param>
/// <param name="Password">用户密码。</param>
/// <param name="IsBusy">是否正在登录。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="CanSubmit">是否允许提交。</param>
/// <param name="LoginStatus">登录状态说明文本。</param>
public sealed record LoginState(
    string UserName,
    string Password,
    bool IsBusy,
    string? ErrorMessage,
    bool CanSubmit,
    string LoginStatus) : IMviState
{
    /// <summary>
    /// 获取空初始状态。
    /// </summary>
    /// <remarks>
    /// 各 sample 可根据演示需要自行构造预填初始状态，而非使用此属性。
    /// </remarks>
    public static LoginState Initial { get; } = new(
        string.Empty,
        string.Empty,
        false,
        null,
        false,
        string.Empty);
}
