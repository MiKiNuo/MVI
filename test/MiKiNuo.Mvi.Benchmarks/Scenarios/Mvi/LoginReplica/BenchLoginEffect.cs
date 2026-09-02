using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景副作用。
/// </summary>
public abstract partial record BenchLoginEffect : IMviEffect
{
    /// <summary>
    /// 表示执行假认证副作用，携带凭证快照。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="Password">密码。</param>
    public sealed partial record PerformLogin(string UserName, string Password) : BenchLoginEffect;
}
