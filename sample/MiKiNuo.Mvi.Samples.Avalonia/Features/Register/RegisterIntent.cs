namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
/// <summary>注册页的用户意图；异步结果不伪装成用户意图。</summary>
public abstract record RegisterIntent : IMviIntent
{
    /// <summary>提交交互发生时捕获的完整输入。</summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="Email">邮箱。</param>
    /// <param name="Password">密码。</param>
    /// <param name="ConfirmPassword">确认密码。</param>
    public sealed record Submit(string UserName, string Email, string Password, string ConfirmPassword) : RegisterIntent
    {
        /// <summary>诊断仅展示操作名，不输出凭据。</summary>
        /// <returns>无敏感值的操作名。</returns>
        public override string ToString() => "Register.Submit";
    }
    /// <summary>返回登录。</summary>
    public sealed record GoLogin : RegisterIntent;
}
