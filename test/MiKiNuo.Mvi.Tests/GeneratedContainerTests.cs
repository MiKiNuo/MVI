using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示源生成容器测试。
/// </summary>
public sealed class GeneratedContainerTests
{
    /// <summary>
    /// 验证容器可以解析登录 ViewModel 和 ViewRegistry。
    /// </summary>
    [Test]
    public async Task Resolve_Should_ReturnRegisteredServicesAsync()
    {
        SampleGeneratedContainer container = new();

        LoginViewModel loginViewModel = container.Resolve<LoginViewModel>();
        AppShellViewModel shellViewModel = container.Resolve<AppShellViewModel>();
        IMviViewRegistry viewRegistry = container.Resolve<IMviViewRegistry>();

        await Assert.That(loginViewModel).IsNotNull();
        await Assert.That(shellViewModel).IsNotNull();
        await Assert.That(viewRegistry).IsNotNull();
    }

    /// <summary>
    /// 验证泛型容器源生成器可以读取 DiService 命名参数并按接口解析服务。
    /// </summary>
    [Test]
    public async Task GenericContainer_Should_ResolveDiServiceByConfiguredServiceTypeAsync()
    {
        GeneratedMviContainer container = new();

        IAuthService authService = container.Resolve<IAuthService>();
        IMviServiceGraph serviceGraph = (IMviServiceGraph)container;
        MviServiceDescriptor descriptor = serviceGraph.ServiceDescriptors
            .Single(static item => item.ServiceType == typeof(IAuthService));

        await Assert.That(authService).IsTypeOf<FakeAuthService>();
        await Assert.That(descriptor.ImplementationType).IsEqualTo(typeof(FakeAuthService));
    }

    /// <summary>
    /// 验证生成容器中的 Dashboard Mediator 会把菜单导航请求应用到父 Dashboard 状态。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_Should_UpdateDashboardCurrentPageAsync()
    {
        SampleGeneratedContainer container = new();

        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        DashboardMenuViewModel menu = (DashboardMenuViewModel)dashboard.MenuViewModel;

        string[] menuKeys = ["住院床位", "检验医嘱", "药房库存", "运营质控", "架构验证中心", "门诊工作站"];
        foreach (string menuKey in menuKeys)
        {
            menu.SelectedMenuKey = menuKey;
            await Task.Delay(100);

            await Assert.That(menu.SelectedMenuKey).IsEqualTo(menuKey);
            await Assert.That(dashboard.CurrentPageTitle).IsEqualTo(menuKey);
        }
    }
}
