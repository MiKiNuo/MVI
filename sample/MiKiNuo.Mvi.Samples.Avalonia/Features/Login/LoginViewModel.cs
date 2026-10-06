namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
/// <summary>登录绑定契约；没有业务代码、构造函数或转发方法。</summary>
public sealed partial class LoginViewModel : MviViewModelBase<LoginState, LoginIntent>
{
    /// <summary>获取或设置用户名，仅保存在本地输入缓冲。</summary>
    [MviBind]
    public partial string UserName { get; set; }
    /// <summary>获取或设置密码，仅保存在本地输入缓冲。</summary>
    [MviBind(Sensitive = true)]
    public partial string Password { get; set; }
    /// <summary>获取提交命令，由生成器捕获输入并构造 Intent。</summary>
    [MviCommand(typeof(LoginIntent.Submit), nameof(UserName), nameof(Password))]
    public partial IMviAsyncCommand SubmitCommand { get; }
    /// <summary>获取注册账号命令。</summary>
    [MviCommand(typeof(LoginIntent.GoRegister))]
    public partial IMviAsyncCommand GoRegisterCommand { get; }
    /// <summary>获取忘记密码命令。</summary>
    [MviCommand(typeof(LoginIntent.GoResetPassword))]
    public partial IMviAsyncCommand GoResetPasswordCommand { get; }
}
