namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>登录页的用户意图；异步结果不伪装成用户意图。</summary>
public abstract record LoginIntent : IMviIntent
{
    /// <summary>提交交互发生时捕获的完整输入。</summary>
    /// <param name="UserName">用户名。</param>
    /// <param name="Password">密码。</param>
    public sealed record Submit(string UserName, string Password) : LoginIntent
    {
        /// <summary>诊断仅展示操作名，不输出凭据。</summary>
        /// <returns>无敏感值的操作名。</returns>
        public override string ToString() => "Login.Submit";
    }
    /// <summary>注册账号。</summary>
    public sealed record GoRegister : LoginIntent;
    /// <summary>忘记密码。</summary>
    public sealed record GoResetPassword : LoginIntent;
}
