using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 [MviFeature] 生成容器的生命周期与装配回归测试。
/// </summary>
public sealed class DiContainerLifetimeTests
{
    /// <summary>
    /// 验证容器可解析 [DiService] 注册的单例服务。
    /// </summary>
    [Test]
    public async Task Container_Should_ResolveSingletonServiceAsync()
    {
        GeneratedMviContainer container = new();

        IAuthService first = container.Resolve<IAuthService>();
        IAuthService second = container.Resolve<IAuthService>();

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
        await Assert.That(first).IsTypeOf<HttpAuthService>();
    }

    /// <summary>
    /// 验证 Feature 的 Store 与 ViewModel 由容器装配且为单例。
    /// </summary>
    [Test]
    public async Task Container_Should_AssembleFeatureAsSingletonsAsync()
    {
        GeneratedMviContainer container = new();

        IMviStore<LoginState, LoginIntent, LoginEffect> firstStore =
            container.Resolve<IMviStore<LoginState, LoginIntent, LoginEffect>>();
        IMviStore<LoginState, LoginIntent, LoginEffect> secondStore =
            container.Resolve<IMviStore<LoginState, LoginIntent, LoginEffect>>();
        LoginViewModel firstViewModel = container.Resolve<LoginViewModel>();
        LoginViewModel secondViewModel = container.Resolve<LoginViewModel>();

        await Assert.That(ReferenceEquals(firstStore, secondStore)).IsTrue();
        await Assert.That(ReferenceEquals(firstViewModel, secondViewModel)).IsTrue();
    }

    /// <summary>
    /// 验证 UnitEffect Feature（应用壳）自动接入空副作用分发器并可正常派发。
    /// </summary>
    [Test]
    public async Task Container_Should_AssembleUnitEffectFeatureAsync()
    {
        GeneratedMviContainer container = new();

        IMviStore<AppShellState, AppShellIntent, UnitEffect> shellStore =
            container.Resolve<IMviStore<AppShellState, AppShellIntent, UnitEffect>>();

        await shellStore.DispatchAsync(new AppShellIntent.ShowRegister());

        await Assert.That(shellStore.CurrentState.CurrentPage).IsEqualTo(ShellPage.Register);
    }

    /// <summary>
    /// 验证主页 ViewModel 的兄弟 Store 构造参数由容器解析。
    /// </summary>
    [Test]
    public async Task Container_Should_ResolveViewModelWithSiblingStoreDependencyAsync()
    {
        GeneratedMviContainer container = new();

        HomeViewModel homeViewModel = container.Resolve<HomeViewModel>();

        await Assert.That(homeViewModel).IsNotNull();
        await Assert.That(homeViewModel.DisplayName).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// 验证容器内置中介者可通过属性与 Resolve 两种方式访问同一实例。
    /// </summary>
    [Test]
    public async Task Container_Should_ExposeSharedMediatorAsync()
    {
        GeneratedMviContainer container = new();

        IMviMediator resolved = container.Resolve<IMviMediator>();

        await Assert.That(ReferenceEquals(resolved, container.Mediator)).IsTrue();
    }

    /// <summary>
    /// 验证容器暴露服务描述集合。
    /// </summary>
    [Test]
    public async Task Container_Should_ExposeServiceDescriptorsAsync()
    {
        GeneratedMviContainer container = new();

        await Assert.That(
            container.ServiceDescriptors.Any(
                static descriptor => descriptor.ServiceType == typeof(IAuthService)
                    && descriptor.Lifetime == ServiceLifetime.Singleton)).IsTrue();
    }
}
