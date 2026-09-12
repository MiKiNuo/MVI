using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页 ViewModel，仅绑定经中介导航请求更新的本地状态。
/// </summary>
public sealed partial class HomeViewModel
    : MviViewModelBase<HomeState, HomeIntent, HomeEffect>
{

    /// <summary>
    /// 初始化主页 ViewModel。
    /// </summary>
    /// <param name="store">主页状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器。</param>
    public HomeViewModel(
        IMviStore<HomeState, HomeIntent, HomeEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取已登录用户显示名。
    /// </summary>
    [MviBind(nameof(HomeState.DisplayName), BindingMode = MviBindingMode.OneWay)]
    public partial string DisplayName { get; private set; }

    /// <summary>
    /// 获取退出登录命令。
    /// </summary>
    [MviCommand(typeof(HomeIntent.Logout))]
    public partial IMviAsyncCommand LogoutCommand { get; private set; }

}
