using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景意图。
/// </summary>
public abstract partial record BenchLoginIntent : IMviIntent
{
    /// <summary>
    /// 表示修改用户名意图。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    public sealed partial record ChangeUserName(string UserName) : BenchLoginIntent;

    /// <summary>
    /// 表示修改密码意图。
    /// </summary>
    /// <param name="Password">密码。</param>
    public sealed partial record ChangePassword(string Password) : BenchLoginIntent;

    /// <summary>
    /// 表示提交登录意图。
    /// </summary>
    public sealed partial record Submit : BenchLoginIntent;

    /// <summary>
    /// 表示登录成功回流意图。
    /// </summary>
    /// <param name="DisplayName">用户显示名。</param>
    public sealed partial record Succeeded(string DisplayName) : BenchLoginIntent;

    /// <summary>
    /// 表示登录失败回流意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record Failed(string ErrorMessage) : BenchLoginIntent;
}
