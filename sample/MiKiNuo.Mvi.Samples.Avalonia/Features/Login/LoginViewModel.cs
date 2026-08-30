using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页 ViewModel。
/// </summary>
public sealed partial class LoginViewModel
    : MviViewModelBase<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 初始化登录页 ViewModel。
    /// </summary>
    /// <param name="store">登录状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public LoginViewModel(
        IMviStore<LoginState, LoginIntent, LoginEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    [MviBind(
        nameof(LoginState.UserName),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(LoginIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    /// <summary>
    /// 获取或设置密码。
    /// </summary>
    [MviBind(
        nameof(LoginState.Password),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(LoginIntent.ChangePassword))]
    public partial string Password { get; set; }

    /// <summary>
    /// 获取是否正在登录。
    /// </summary>
    [MviBind(nameof(LoginState.IsBusy))]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    /// 获取错误消息。
    /// </summary>
    [MviBind(nameof(LoginState.ErrorMessage))]
    public partial string? ErrorMessage { get; private set; }

    /// <summary>
    /// 获取是否允许提交。
    /// </summary>
    [MviBind(nameof(LoginState.CanSubmit))]
    public partial bool CanSubmit { get; private set; }

    /// <summary>
    /// 获取提交登录命令。
    /// </summary>
    [MviCommand(typeof(LoginIntent.Submit), CanExecuteProperty = nameof(CanSubmit), IsAsync = true)]
    public partial IMviAsyncCommand SubmitCommand { get; private set; }

    /// <summary>
    /// 获取跳转注册页命令。
    /// </summary>
    [MviCommand(typeof(LoginIntent.GoRegister), IsAsync = true)]
    public partial IMviAsyncCommand GoRegisterCommand { get; private set; }
}
