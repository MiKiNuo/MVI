namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
/// <summary>重置密码绑定契约；没有业务代码、构造函数或转发方法。</summary>
public sealed partial class ResetPasswordViewModel : MviViewModelBase<ResetPasswordState, ResetPasswordIntent>
{
    /// <summary>获取或设置用户名，仅保存在本地输入缓冲。</summary>
    [MviBind]
    public partial string UserName { get; set; }
    /// <summary>获取或设置新密码，仅保存在本地输入缓冲。</summary>
    [MviBind(Sensitive = true)]
    public partial string NewPassword { get; set; }
    /// <summary>获取或设置确认新密码，仅保存在本地输入缓冲。</summary>
    [MviBind(Sensitive = true)]
    public partial string ConfirmPassword { get; set; }
    /// <summary>获取提交命令，由生成器捕获输入并构造 Intent。</summary>
    [MviCommand(typeof(ResetPasswordIntent.Submit), nameof(UserName), nameof(NewPassword), nameof(ConfirmPassword))]
    public partial IMviAsyncCommand SubmitCommand { get; }
    /// <summary>获取返回登录命令。</summary>
    [MviCommand(typeof(ResetPasswordIntent.GoLogin))]
    public partial IMviAsyncCommand GoLoginCommand { get; }
}
