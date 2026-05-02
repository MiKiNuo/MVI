namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示认证服务。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 登录。
    /// </summary>
    /// <param name="userName">用户账号。</param>
    /// <param name="password">用户密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>登录结果。</returns>
    public ValueTask<LoginResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);
}
