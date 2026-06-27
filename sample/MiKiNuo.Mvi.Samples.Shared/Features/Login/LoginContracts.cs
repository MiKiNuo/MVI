                     using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiKiNuo.Mvi.Samples.Shared.Features.Login;

/// <summary>
/// 表示登录用户资料的跨平台契约。
/// </summary>
/// <remarks>
/// 各平台 sample 实现此接口以提供平台特定的用户资料（如 Avalonia 的显示名、Godot 的玩家资料）。
/// </remarks>
public interface ILoginProfile
{
    /// <summary>
    /// 获取用于显示的用户名称。
    /// </summary>
    public string DisplayName { get; }
}

/// <summary>
/// 表示登录结果。
/// </summary>
/// <param name="IsSuccess">是否成功。</param>
/// <param name="Profile">登录成功的用户资料；失败时为 null。</param>
/// <param name="ErrorMessage">错误消息；成功时为 null。</param>
public sealed record LoginResult(bool IsSuccess, ILoginProfile? Profile, string? ErrorMessage)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="profile">用户资料。</param>
    /// <returns>登录结果。</returns>
    public static LoginResult Success(ILoginProfile profile)
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

/// <summary>
/// 表示认证服务契约。
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
