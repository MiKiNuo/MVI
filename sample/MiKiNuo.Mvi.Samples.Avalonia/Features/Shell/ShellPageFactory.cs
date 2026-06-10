using System;
using System.Collections.Generic;
using MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳顶层页面 ViewModel 的工厂实现。
/// <para>
/// 2 个顶层页面（Login / EventBindingWorkbench）由构造时一次性注入并缓存，
/// 缓存避免每次切换都重新构造。每个页面 VM 各自拥有独立 store，组合根负责生命周期。
/// </para>
/// </summary>
public sealed class ShellPageFactory : IShellPageFactory
{
    private readonly IReadOnlyDictionary<string, object> _pageViewModels;

    /// <summary>
    /// 初始化应用壳顶层页面 ViewModel 工厂。
    /// </summary>
    /// <param name="loginViewModel">登录页 ViewModel。</param>
    /// <param name="eventBindingWorkbenchViewModel">事件绑定 Workbench 页 ViewModel。</param>
    public ShellPageFactory(
        LoginViewModel loginViewModel,
        EventBindingWorkbenchViewModel eventBindingWorkbenchViewModel)
    {
        ArgumentNullException.ThrowIfNull(loginViewModel);
        ArgumentNullException.ThrowIfNull(eventBindingWorkbenchViewModel);

        _pageViewModels = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [ShellPageKeys.Login] = loginViewModel,
            [ShellPageKeys.EventBindingWorkbench] = eventBindingWorkbenchViewModel,
        };
    }

    /// <inheritdoc />
    public object? CreatePage(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        return _pageViewModels.TryGetValue(pageKey, out object? viewModel) ? viewModel : null;
    }
}
