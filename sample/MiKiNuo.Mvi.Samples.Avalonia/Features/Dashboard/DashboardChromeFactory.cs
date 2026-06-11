using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 <see cref="IDashboardChromeFactory"/> 的标准实现。
/// <para>
/// 左侧菜单 <see cref="DashboardMenuViewModel"/> 在工厂构造时一次性实例化并缓存；
/// 顶部头部 <see cref="HeaderViewModel"/> 与显示名绑定，故每次
/// <see cref="CreateHeaderViewModel(string)"/> 按显示名缓存：
/// 同一显示名命中缓存，避免每次重建；不同显示名各持一份实例。
/// </para>
/// </summary>
public sealed class DashboardChromeFactory : IDashboardChromeFactory
{
    private readonly Func<string, HeaderViewModel> _headerFactory;
    private readonly DashboardMenuViewModel _menuViewModel;
    private readonly Dictionary<string, HeaderViewModel> _headerCache = new(StringComparer.Ordinal);

    /// <summary>
    /// 初始化 Dashboard chrome 子组件 ViewModel 工厂。
    /// </summary>
    /// <param name="menuViewModel">左侧菜单子组件 ViewModel（单例缓存）。</param>
    /// <param name="headerFactory">按 displayName 构造头部 <see cref="HeaderViewModel"/> 的委托。
    /// 由组合根提供，因为头部 ViewModel 依赖当前登录显示名。</param>
    public DashboardChromeFactory(
        DashboardMenuViewModel menuViewModel,
        Func<string, HeaderViewModel> headerFactory)
    {
        ArgumentNullException.ThrowIfNull(menuViewModel);
        ArgumentNullException.ThrowIfNull(headerFactory);

        _menuViewModel = menuViewModel;
        _headerFactory = headerFactory;
    }

    /// <inheritdoc />
    public object CreateHeaderViewModel(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        if (!_headerCache.TryGetValue(displayName, out HeaderViewModel? header))
        {
            header = _headerFactory(displayName);
            _headerCache[displayName] = header;
        }

        return header;
    }

    /// <inheritdoc />
    public object CreateMenuViewModel() => _menuViewModel;
}
