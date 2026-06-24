using System.Threading;
using System.Threading.Tasks;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示后端认证服务接口。
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 异步登录。
    /// </summary>
    /// <param name="userName">用户账号。</param>
    /// <param name="password">用户密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>登录结果。</returns>
    public ValueTask<LoginResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);
}
