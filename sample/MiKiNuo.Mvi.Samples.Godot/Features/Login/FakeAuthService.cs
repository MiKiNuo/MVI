using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示模拟后端认证服务。
/// </summary>
public sealed class FakeAuthService : IAuthService
{
    /// <summary>
    /// 异步登录。
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

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return LoginResult.Failure("账号和密码不能为空。");
        }

        if (password.Length < 3)
        {
            return LoginResult.Failure("密码长度至少 3 位。");
        }

        string playerName = string.Create(CultureInfo.InvariantCulture, $"{userName.Trim()} 指挥官");
        PlayerProfile profile = new(playerName, level: 12, gold: 360, stamina: 80);
        return LoginResult.Success(profile);
    }
}
