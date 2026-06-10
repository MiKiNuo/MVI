using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 ViewModel。
/// <para>
/// 不再持有 <c>LoginViewModel</c> / <c>LobbyViewModel</c> 引用；
/// 顶层页面 VM 通过 <see cref="IGameScreenFactory"/> 按 <see cref="CurrentScreen"/> 解析。
/// </para>
/// </summary>
public sealed partial class AppShellViewModel : MviViewModelBase<AppShellState, AppShellIntent, AppShellEffect>
{
    private readonly IGameScreenFactory _screenFactory;

    /// <summary>
    /// 初始化游戏应用壳 ViewModel。
    /// </summary>
    /// <param name="store">应用壳状态存储。</param>
    /// <param name="screenFactory">顶层页面 ViewModel 工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Godot 主线程触发 CanExecuteChanged）。</param>
    public AppShellViewModel(
        IMviStore<AppShellState, AppShellIntent, AppShellEffect> store,
        IGameScreenFactory screenFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(screenFactory);
        _screenFactory = screenFactory;
    }

    /// <summary>
    /// 获取当前顶层页面键。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentScreen))]
    public partial string CurrentScreen { get; private set; }

    /// <summary>
    /// 获取当前页面标题。
    /// </summary>
    [MviBind(nameof(AppShellState.CurrentTitle))]
    public partial string CurrentTitle { get; private set; }

    /// <summary>
    /// 获取应用壳提示消息。
    /// </summary>
    [MviBind(nameof(AppShellState.ShellMessage))]
    public partial string ShellMessage { get; private set; }

    /// <summary>
    /// 按 <see cref="CurrentScreen"/> 通过 <see cref="IGameScreenFactory"/> 解析当前顶层页面 ViewModel。
    /// </summary>
    /// <returns>顶层页面 ViewModel；未识别 <see cref="CurrentScreen"/> 时返回 null。</returns>
    public object? CreateCurrentScreenViewModel()
    {
        return _screenFactory.CreateScreen(CurrentScreen);
    }
}
