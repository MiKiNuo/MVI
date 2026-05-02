using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
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
}
