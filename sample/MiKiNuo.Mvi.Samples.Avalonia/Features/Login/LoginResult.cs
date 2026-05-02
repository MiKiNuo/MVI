namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录结果。
/// </summary>
/// <param name="IsSuccess">是否成功。</param>
/// <param name="DisplayName">显示名称。</param>
/// <param name="ErrorMessage">错误消息。</param>
public sealed record LoginResult(bool IsSuccess, string? DisplayName, string? ErrorMessage)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="displayName">显示名称。</param>
    /// <returns>登录结果。</returns>
    public static LoginResult Success(string displayName)
    {
        return new LoginResult(true, displayName, null);
    }

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="errorMessage">错误消息。</param>
    /// <returns>登录结果。</returns>
    public static LoginResult Failure(string errorMessage)
    {
        return new LoginResult(false, null, errorMessage);
    }
}
