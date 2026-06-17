using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示"登录成功 → 跳转到 Dashboard → 渲染 Dashboard 组合界面"端到端路径的回归测试。
/// <para>
/// 现象：登录成功后界面不显示组合模式（Dashboard）。
/// 假设：登录成功后 <see cref="AppShellViewModel.CurrentPageKey"/> 变为 "Dashboard"，
/// 但 <see cref="ShellPageFactory"/> 字典中没有 "Dashboard" 键，
/// 导致 <c>MainWindow</c> 调用 <see cref="IShellPageFactory.CreatePage"/> 返回 null，
/// <c>RootContent.Content</c> 被设为 null，界面空白。
/// </para>
/// <para>
/// 本测试是 bug 的最小复现：模拟登录成功的导航流程（<c>NavigateToDashboardAsync</c>），
/// 验证 <see cref="IShellPageFactory.CreatePage"/>(<see cref="AppShellViewModel.CurrentPageKey"/>) 必须返回 <see cref="DashboardViewModel"/> 实例。
/// </para>
/// </summary>
public sealed class LoginNavigationRegressionTests
{
    /// <summary>
    /// 验证登录成功导航后，<see cref="IShellPageFactory"/> 能按 "Dashboard" 键解析出 <see cref="DashboardViewModel"/>。
    /// <para>
    /// 这是"登录成功后界面不显示组合模式"症状的最小复现：登录成功后 <c>CurrentPageKey</c> 被设为 "Dashboard"，
    /// <c>MainWindow.RenderCurrentView</c> 调 <c>IShellPageFactory.CreatePage("Dashboard")</c>，若返回 null 则内容区为空。
    /// </para>
    /// </summary>
    [Test]
    public async Task ShellPageFactory_Should_ResolveDashboardAfterLoginNavigationAsync()
    {
        SampleGeneratedContainer container = new();

        await container.NavigateToDashboardAsync("测试用户");

        AppShellViewModel shellViewModel = container.Resolve<AppShellViewModel>();
        IShellPageFactory pageFactory = container.Resolve<IShellPageFactory>();

        await Assert.That(shellViewModel.CurrentPageKey).IsEqualTo("Dashboard");

        object? currentPage = pageFactory.CreatePage(shellViewModel.CurrentPageKey);
        await Assert.That(currentPage).IsNotNull();
        await Assert.That(currentPage).IsTypeOf<DashboardViewModel>();
    }
}
