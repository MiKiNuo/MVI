using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻基准场景 ViewModel：镜像真实登录示例的绑定与命令形态。
/// </summary>
public sealed partial class BenchLoginViewModel
    : MviViewModelBase<BenchLoginState, BenchLoginIntent, BenchLoginEffect>
{
    /// <summary>
    /// 初始化登录复刻基准场景 ViewModel。
    /// </summary>
    /// <param name="store">登录复刻状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public BenchLoginViewModel(
        IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    [MviBind(nameof(BenchLoginState.UserName), IntentType = typeof(BenchLoginIntent.ChangeUserName))]
    public partial string UserName { get; set; }

    /// <summary>
    /// 获取或设置密码。
    /// </summary>
    [MviBind(nameof(BenchLoginState.Password), IntentType = typeof(BenchLoginIntent.ChangePassword))]
    public partial string Password { get; set; }

    /// <summary>
    /// 获取是否正在登录。
    /// </summary>
    [MviBind(nameof(BenchLoginState.IsBusy), BindingMode = MviBindingMode.OneWay)]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    /// 获取错误消息。
    /// </summary>
    [MviBind(nameof(BenchLoginState.ErrorMessage), BindingMode = MviBindingMode.OneWay)]
    public partial string? ErrorMessage { get; private set; }

    /// <summary>
    /// 获取是否允许提交。
    /// </summary>
    [MviBind(nameof(BenchLoginState.CanSubmit), BindingMode = MviBindingMode.OneWay)]
    public partial bool CanSubmit { get; private set; }

    /// <summary>
    /// 获取提交登录命令。
    /// </summary>
    [MviCommand(typeof(BenchLoginIntent.Submit), CanExecuteProperty = nameof(CanSubmit))]
    public partial IMviAsyncCommand SubmitCommand { get; private set; }
}
