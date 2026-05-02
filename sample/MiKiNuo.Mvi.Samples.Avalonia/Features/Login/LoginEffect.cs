using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面副作用。
/// </summary>
public abstract partial record LoginEffect : IMviEffect
{
    /// <summary>
    /// 表示请求登录副作用。
    /// </summary>
    /// <param name="UserName">用户账号。</param>
    /// <param name="Password">用户密码。</param>
    public sealed partial record RequestLogin(string UserName, string Password) : LoginEffect;

    /// <summary>
    /// 表示导航到 Dashboard 副作用。
    /// </summary>
    /// <param name="DisplayName">显示名称。</param>
    public sealed partial record NavigateToDashboard(string DisplayName) : LoginEffect;
}
