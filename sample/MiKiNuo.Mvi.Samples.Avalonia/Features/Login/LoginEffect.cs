using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页副作用。
/// </summary>
public abstract partial record LoginEffect : IMviEffect
{
    /// <summary>
    /// 表示执行联网登录副作用，携带凭证快照。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="Password">密码。</param>
    public sealed partial record PerformLogin(string UserName, string Password) : LoginEffect;

    /// <summary>
    /// 表示跳转到注册页副作用。
    /// </summary>
    public sealed partial record ShowRegisterPage : LoginEffect;

    /// <summary>
    /// 表示跳转到重置密码页副作用。
    /// </summary>
    public sealed partial record ShowResetPasswordPage : LoginEffect;

    /// <summary>
    /// 表示跳转到主页副作用。
    /// </summary>
    /// <param name="DisplayName">用户显示名。</param>
    public sealed partial record ShowHomePage(string DisplayName) : LoginEffect;
}
