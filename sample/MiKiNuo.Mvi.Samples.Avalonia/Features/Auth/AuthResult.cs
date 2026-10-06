namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>
/// 表示认证服务调用结果。
/// </summary>
/// <param name="IsSuccess">是否成功。</param>
/// <param name="DisplayName">成功时的用户显示名。</param>
/// <param name="ErrorMessage">失败时的错误消息。</param>
public sealed record AuthResult(
    bool IsSuccess,
    string? DisplayName,
    string? ErrorMessage)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="displayName">用户显示名。</param>
    /// <returns>成功结果。</returns>
    public static AuthResult Success(string displayName) => new(true, displayName, null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="errorMessage">错误消息。</param>
    /// <returns>失败结果。</returns>
    public static AuthResult Failure(string errorMessage) => new(false, null, errorMessage);
}
