using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示基准用假认证服务契约：确定性结果、无任何 IO，保证基准可重复。
/// </summary>
public interface IBenchAuthService
{
    /// <summary>
    /// 执行假认证。
    /// </summary>
    /// <param name="userName">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>认证结果。</returns>
    public ValueTask<BenchAuthResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken);
}

/// <summary>
/// 表示基准用认证结果。
/// </summary>
/// <param name="IsSuccess">是否成功。</param>
/// <param name="DisplayName">成功时的显示名。</param>
/// <param name="ErrorMessage">失败时的错误消息。</param>
public sealed record BenchAuthResult(
    bool IsSuccess,
    string? DisplayName,
    string? ErrorMessage);

/// <summary>
/// 表示基准用假认证服务实现：口令为 fail 时失败，其余一律成功。
/// </summary>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IBenchAuthService))]
public sealed class BenchAuthService : IBenchAuthService
{
    /// <summary>
    /// 执行确定性假认证：口令 fail 失败，否则成功。
    /// </summary>
    /// <param name="userName">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>认证结果。</returns>
    public ValueTask<BenchAuthResult> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.Equals(password, "fail", StringComparison.Ordinal))
        {
            return ValueTask.FromResult(new BenchAuthResult(false, null, "认证失败。"));
        }

        return ValueTask.FromResult(new BenchAuthResult(true, "Bench User", null));
    }
}
