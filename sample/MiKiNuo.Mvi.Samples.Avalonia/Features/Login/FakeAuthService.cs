using MiKiNuo.Mvi.Domain.DI;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示示例认证服务。
/// </summary>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IAuthService))]
public sealed class FakeAuthService : IAuthService
{
    /// <summary>
    /// 登录。
    /// </summary>
    /// <param name="userName">用户账号。</param>
    /// <param name="password">用户密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>登录结果。</returns>
    public async ValueTask<LoginResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken).ConfigureAwait(false);

        if (string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase)
            && password == "123456")
        {
            return LoginResult.Success("架构师 Admin");
        }

        return LoginResult.Failure("账号或密码错误。示例账号：admin / 123456");
    }
}
