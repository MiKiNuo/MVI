namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>重置密码页的用户意图；异步结果不伪装成用户意图。</summary>
public abstract record ResetPasswordIntent : IMviIntent
{
    /// <summary>提交交互发生时捕获的完整输入。</summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="NewPassword">新密码。</param>
    /// <param name="ConfirmPassword">确认新密码。</param>
    public sealed record Submit(string UserName, string NewPassword, string ConfirmPassword) : ResetPasswordIntent
    {
        /// <summary>诊断仅展示操作名，不输出凭据。</summary>
        /// <returns>无敏感值的操作名。</returns>
        public override string ToString() => "ResetPassword.Submit";
    }
    /// <summary>返回登录。</summary>
    public sealed record GoLogin : ResetPasswordIntent;
}
