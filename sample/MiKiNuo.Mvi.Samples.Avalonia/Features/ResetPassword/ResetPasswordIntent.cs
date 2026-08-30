using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页意图。
/// </summary>
public abstract partial record ResetPasswordIntent : IMviIntent
{
    /// <summary>
    /// 表示修改用户名意图。
    /// </summary>
    /// <param name="UserName">用户名。</param>
    public sealed partial record ChangeUserName(string UserName) : ResetPasswordIntent;

    /// <summary>
    /// 表示修改新密码意图。
    /// </summary>
    /// <param name="NewPassword">新密码。</param>
    public sealed partial record ChangeNewPassword(string NewPassword) : ResetPasswordIntent;

    /// <summary>
    /// 表示修改确认密码意图。
    /// </summary>
    /// <param name="ConfirmPassword">确认新密码。</param>
    public sealed partial record ChangeConfirmPassword(string ConfirmPassword) : ResetPasswordIntent;

    /// <summary>
    /// 表示提交重置密码意图。
    /// </summary>
    public sealed partial record Submit : ResetPasswordIntent;

    /// <summary>
    /// 表示重置成功回流意图。
    /// </summary>
    public sealed partial record Succeeded : ResetPasswordIntent;

    /// <summary>
    /// 表示重置失败回流意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record Failed(string ErrorMessage) : ResetPasswordIntent;

    /// <summary>
    /// 表示返回登录页意图。
    /// </summary>
    public sealed partial record GoLogin : ResetPasswordIntent;
}
