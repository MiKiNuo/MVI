using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页 ViewModel。
/// </summary>
public sealed partial class RegisterViewModel
    : MviViewModelBase<RegisterState, RegisterIntent, RegisterEffect>
{
    /// <summary>
    /// 初始化注册页 ViewModel。
    /// </summary>
    /// <param name="store">注册状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public RegisterViewModel(
        IMviStore<RegisterState, RegisterIntent, RegisterEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    [MviBind(
        nameof(RegisterState.UserName),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(RegisterIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    /// <summary>
    /// 获取或设置邮箱。
    /// </summary>
    [MviBind(
        nameof(RegisterState.Email),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(RegisterIntent.ChangeEmail))]
    public partial string Email { get; set; }

    /// <summary>
    /// 获取或设置密码。
    /// </summary>
    [MviBind(
        nameof(RegisterState.Password),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(RegisterIntent.ChangePassword))]
    public partial string Password { get; set; }

    /// <summary>
    /// 获取或设置确认密码。
    /// </summary>
    [MviBind(
        nameof(RegisterState.ConfirmPassword),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(RegisterIntent.ChangeConfirmPassword))]
    public partial string ConfirmPassword { get; set; }

    /// <summary>
    /// 获取是否正在注册。
    /// </summary>
    [MviBind(nameof(RegisterState.IsBusy))]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    /// 获取错误消息。
    /// </summary>
    [MviBind(nameof(RegisterState.ErrorMessage))]
    public partial string? ErrorMessage { get; private set; }

    /// <summary>
    /// 获取是否允许提交。
    /// </summary>
    [MviBind(nameof(RegisterState.CanSubmit))]
    public partial bool CanSubmit { get; private set; }

    /// <summary>
    /// 获取提交注册命令。
    /// </summary>
    [MviCommand(typeof(RegisterIntent.Submit), CanExecuteProperty = nameof(CanSubmit), IsAsync = true)]
    public partial IMviAsyncCommand SubmitCommand { get; private set; }

    /// <summary>
    /// 获取跳转登录页命令。
    /// </summary>
    [MviCommand(typeof(RegisterIntent.GoLogin), IsAsync = true)]
    public partial IMviAsyncCommand GoLoginCommand { get; private set; }
}
