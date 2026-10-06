namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅只声明一个命令，其余显示直接读取 State。</summary>
public sealed partial class HomeViewModel : MviViewModelBase<HomeState, HomeIntent>
{
    /// <summary>获取退出命令。</summary>
    [MviCommand(typeof(HomeIntent.Logout))]
    public partial IMviAsyncCommand LogoutCommand { get; }
}
