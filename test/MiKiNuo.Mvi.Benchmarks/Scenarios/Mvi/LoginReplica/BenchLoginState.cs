using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景状态：镜像真实登录示例的字段形态。
/// </summary>
/// <param name="UserName">用户名。</param>
/// <param name="Password">密码。</param>
/// <param name="IsBusy">是否正在登录。</param>
/// <param name="ErrorMessage">错误消息。</param>
/// <param name="CanSubmit">是否允许提交。</param>
/// <param name="DisplayName">登录成功后的显示名。</param>
public sealed record BenchLoginState(
    string UserName,
    string Password,
    bool IsBusy,
    string? ErrorMessage,
    bool CanSubmit,
    string? DisplayName) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static BenchLoginState Initial { get; } = new(
        string.Empty,
        string.Empty,
        false,
        null,
        false,
        null);
}
