namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示 <see cref="ILobbyChromeFactory"/> 的标准实现。
/// <para>
/// 3 个常驻 chrome 子 VM 在工厂构造时一次性实例化并缓存。
/// 与 <see cref="LobbyPanelFactory"/> 区别：本工厂处理的是 3 个常驻 chrome 子 VM，
/// 那个工厂处理的是 5 个互斥面板 VM——两个工厂不重叠，共同支撑 <see cref="LobbyViewModel"/>。
/// </para>
/// </summary>
public sealed class LobbyChromeFactory : ILobbyChromeFactory
{
    private readonly object _headerViewModel;
    private readonly object _menuViewModel;
    private readonly object _activityLogViewModel;

    /// <summary>
    /// 初始化游戏大厅 chrome 子组件 ViewModel 工厂。
    /// </summary>
    /// <param name="headerViewModel">玩家头部子组件 ViewModel。</param>
    /// <param name="menuViewModel">大厅菜单子组件 ViewModel。</param>
    /// <param name="activityLogViewModel">活动日志子组件 ViewModel。</param>
    public LobbyChromeFactory(
        PlayerHeaderViewModel headerViewModel,
        LobbyMenuViewModel menuViewModel,
        ActivityLogViewModel activityLogViewModel)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(menuViewModel);
        ArgumentNullException.ThrowIfNull(activityLogViewModel);

        _headerViewModel = headerViewModel;
        _menuViewModel = menuViewModel;
        _activityLogViewModel = activityLogViewModel;
    }

    /// <inheritdoc />
    public object CreateHeaderViewModel() => _headerViewModel;

    /// <inheritdoc />
    public object CreateMenuViewModel() => _menuViewModel;

    /// <inheritdoc />
    public object CreateActivityLogViewModel() => _activityLogViewModel;
}
