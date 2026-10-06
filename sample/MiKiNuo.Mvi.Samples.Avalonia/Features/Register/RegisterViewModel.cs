namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
/// <summary>注册绑定契约；没有业务代码、构造函数或转发方法。</summary>
public sealed partial class RegisterViewModel : MviViewModelBase<RegisterState, RegisterIntent>
{
    /// <summary>获取或设置用户名，仅保存在本地输入缓冲。</summary>
    [MviBind]
    public partial string UserName { get; set; }
    /// <summary>获取或设置邮箱，仅保存在本地输入缓冲。</summary>
    [MviBind]
    public partial string Email { get; set; }
    /// <summary>获取或设置密码，仅保存在本地输入缓冲。</summary>
    [MviBind(Sensitive = true)]
    public partial string Password { get; set; }
    /// <summary>获取或设置确认密码，仅保存在本地输入缓冲。</summary>
    [MviBind(Sensitive = true)]
    public partial string ConfirmPassword { get; set; }
    /// <summary>获取提交命令，由生成器捕获输入并构造 Intent。</summary>
    [MviCommand(typeof(RegisterIntent.Submit), nameof(UserName), nameof(Email), nameof(Password), nameof(ConfirmPassword))]
    public partial IMviAsyncCommand SubmitCommand { get; }
    /// <summary>获取返回登录命令。</summary>
    [MviCommand(typeof(RegisterIntent.GoLogin))]
    public partial IMviAsyncCommand GoLoginCommand { get; }
}
