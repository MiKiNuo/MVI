using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页 ViewModel。
/// </summary>
public sealed partial class ResetPasswordViewModel
    : MviViewModelBase<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect>
{
    /// <summary>
    /// 初始化重置密码页 ViewModel。
    /// </summary>
    /// <param name="store">重置密码状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public ResetPasswordViewModel(
        IMviStore<ResetPasswordState, ResetPasswordIntent, ResetPasswordEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.UserName), IntentType = typeof(ResetPasswordIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    /// <summary>
    /// 获取或设置新密码。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.NewPassword), IntentType = typeof(ResetPasswordIntent.ChangeNewPassword))]
    public partial string NewPassword { get; set; }

    /// <summary>
    /// 获取或设置确认密码。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.ConfirmPassword), IntentType = typeof(ResetPasswordIntent.ChangeConfirmPassword))]
    public partial string ConfirmPassword { get; set; }

    /// <summary>
    /// 获取是否正在提交。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.IsBusy), BindingMode = MviBindingMode.OneWay)]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    /// 获取错误消息。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.ErrorMessage), BindingMode = MviBindingMode.OneWay)]
    public partial string? ErrorMessage { get; private set; }

    /// <summary>
    /// 获取是否允许提交。
    /// </summary>
    [MviBind(nameof(ResetPasswordState.CanSubmit), BindingMode = MviBindingMode.OneWay)]
    public partial bool CanSubmit { get; private set; }

    /// <summary>
    /// 获取提交重置密码命令。
    /// </summary>
    [MviCommand(typeof(ResetPasswordIntent.Submit), CanExecuteProperty = nameof(CanSubmit))]
    public partial IMviAsyncCommand SubmitCommand { get; private set; }

    /// <summary>
    /// 获取返回登录页命令。
    /// </summary>
    [MviCommand(typeof(ResetPasswordIntent.GoLogin))]
    public partial IMviAsyncCommand GoLoginCommand { get; private set; }
}
