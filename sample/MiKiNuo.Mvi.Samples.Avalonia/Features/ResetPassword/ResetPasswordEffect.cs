using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页副作用。
/// </summary>
public abstract partial record ResetPasswordEffect : IMviEffect
{
    /// <summary>
    /// 表示执行联网重置密码副作用，携带用户名与新密码快照。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="NewPassword">新密码。</param>
    public sealed partial record PerformResetPassword(string UserName, string NewPassword) : ResetPasswordEffect;

    /// <summary>
    /// 表示返回登录页副作用。
    /// </summary>
    public sealed partial record ShowLoginPage : ResetPasswordEffect;
}
