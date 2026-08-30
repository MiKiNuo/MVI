namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>
/// 表示认证服务契约。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 联网登录。
    /// </summary>
    /// <param name="userName">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>认证结果。</returns>
    public Task<AuthResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// 联网注册。
    /// </summary>
    /// <param name="userName">用户名。</param>
    /// <param name="email">邮箱。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>认证结果。</returns>
    public Task<AuthResult> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken);
}
