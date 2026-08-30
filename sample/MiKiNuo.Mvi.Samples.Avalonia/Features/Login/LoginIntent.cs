using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页意图。
/// </summary>
public abstract partial record LoginIntent : IMviIntent
{
    /// <summary>
    /// 表示修改用户名意图。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    public sealed partial record ChangeUserName(string UserName) : LoginIntent;

    /// <summary>
    /// 表示修改密码意图。
    /// </summary>
    /// <param name="Password">密码。</param>
    public sealed partial record ChangePassword(string Password) : LoginIntent;

    /// <summary>
    /// 表示提交登录意图。
    /// </summary>
    public sealed partial record Submit : LoginIntent;

    /// <summary>
    /// 表示登录成功回流意图。
    /// </summary>
    /// <param name="DisplayName">用户显示名。</param>
    public sealed partial record Succeeded(string DisplayName) : LoginIntent;

    /// <summary>
    /// 表示登录失败回流意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record Failed(string ErrorMessage) : LoginIntent;

    /// <summary>
    /// 表示跳转到注册页意图。
    /// </summary>
    public sealed partial record GoRegister : LoginIntent;

    /// <summary>
    /// 表示跳转到重置密码页意图。
    /// </summary>
    public sealed partial record GoResetPassword : LoginIntent;
}
