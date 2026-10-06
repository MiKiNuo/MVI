namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

/// <summary>第三方测试服务端口；Handler 依赖此接口，ViewModel 不依赖网络。</summary>
public interface IAuthService
{
    /// <summary>通过第三方预置测试账号登录。</summary>
    /// <param name="userName">测试用户名。</param>
    /// <param name="password">测试密码，不应使用真实凭据。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>认证结果。</returns>
    public Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken);

    /// <summary>调用用户新增测试接口；该接口不会持久化新账号。</summary>
    /// <param name="userName">演示用户名。</param>
    /// <param name="email">演示邮箱。</param>
    /// <param name="password">演示密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>模拟注册结果。</returns>
    public Task<AuthResult> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken);

    /// <summary>沿用 main 的 PUT /users/1 演示；不提供真实密码恢复、验证码或持久化。</summary>
    /// <param name="userName">界面演示用户名，不改变固定的测试用户编号 1。</param>
    /// <param name="newPassword">仅发送到公共测试接口的演示密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>模拟更新结果。</returns>
    public Task<AuthResult> ResetPasswordAsync(string userName, string newPassword, CancellationToken cancellationToken);
}
