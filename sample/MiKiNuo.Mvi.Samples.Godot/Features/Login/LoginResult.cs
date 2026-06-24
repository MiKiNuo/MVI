namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示登录结果。
/// </summary>
/// <param name="IsSuccess">是否成功。</param>
/// <param name="Profile">玩家资料。</param>
/// <param name="ErrorMessage">错误消息。</param>
public sealed record LoginResult(bool IsSuccess, PlayerProfile? Profile, string? ErrorMessage)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="profile">玩家资料。</param>
    /// <returns>登录结果。</returns>
    public static LoginResult Success(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new LoginResult(true, profile, null);
    }

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="errorMessage">错误消息。</param>
    /// <returns>登录结果。</returns>
    public static LoginResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new LoginResult(false, null, errorMessage);
    }
}
