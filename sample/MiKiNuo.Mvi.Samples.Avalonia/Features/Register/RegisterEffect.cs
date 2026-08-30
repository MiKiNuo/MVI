using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页副作用。
/// </summary>
public abstract partial record RegisterEffect : IMviEffect
{
    /// <summary>
    /// 表示执行联网注册副作用，携带表单快照。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="Email">邮箱。</param>
    /// <param name="Password">密码。</param>
    public sealed partial record PerformRegister(string UserName, string Email, string Password) : RegisterEffect;

    /// <summary>
    /// 表示跳转到登录页副作用。
    /// </summary>
    public sealed partial record ShowLoginPage : RegisterEffect;
}
