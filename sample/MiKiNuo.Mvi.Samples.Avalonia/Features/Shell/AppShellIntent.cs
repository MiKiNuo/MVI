using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳意图。
/// </summary>
public abstract partial record AppShellIntent : IMviIntent
{
    /// <summary>
    /// 表示导航到登录页意图。
    /// </summary>
    public sealed partial record ShowLogin : AppShellIntent;

    /// <summary>
    /// 表示导航到注册页意图。
    /// </summary>
    public sealed partial record ShowRegister : AppShellIntent;

    /// <summary>
    /// 表示导航到重置密码页意图。
    /// </summary>
    public sealed partial record ShowResetPassword : AppShellIntent;

    /// <summary>
    /// 表示导航到主页意图。
    /// </summary>
    /// <param name="DisplayName">用户显示名。</param>
    public sealed partial record ShowHome(string DisplayName) : AppShellIntent;
}
