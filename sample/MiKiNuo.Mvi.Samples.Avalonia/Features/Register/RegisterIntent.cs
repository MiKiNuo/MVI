using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页意图。
/// </summary>
public abstract partial record RegisterIntent : IMviIntent
{
    /// <summary>
    /// 表示修改用户名意图。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    public sealed partial record ChangeUserName(string UserName) : RegisterIntent;

    /// <summary>
    /// 表示修改邮箱意图。
    /// </summary>
    /// <param name="Email">邮箱。</param>
    public sealed partial record ChangeEmail(string Email) : RegisterIntent;

    /// <summary>
    /// 表示修改密码意图。
    /// </summary>
    /// <param name="Password">密码。</param>
    public sealed partial record ChangePassword(string Password) : RegisterIntent;

    /// <summary>
    /// 表示修改确认密码意图。
    /// </summary>
    /// <param name="ConfirmPassword">确认密码。</param>
    public sealed partial record ChangeConfirmPassword(string ConfirmPassword) : RegisterIntent;

    /// <summary>
    /// 表示提交注册意图。
    /// </summary>
    public sealed partial record Submit : RegisterIntent;

    /// <summary>
    /// 表示注册成功回流意图。
    /// </summary>
    public sealed partial record Succeeded : RegisterIntent;

    /// <summary>
    /// 表示注册失败回流意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record Failed(string ErrorMessage) : RegisterIntent;

    /// <summary>
    /// 表示跳转到登录页意图。
    /// </summary>
    public sealed partial record GoLogin : RegisterIntent;
}
