using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示编译期 DI 容器生命周期测试。
/// </summary>
public sealed class DiContainerLifetimeTests
{
    /// <summary>
    /// 验证根容器可以通过泛型和 Type 两种方式解析单例服务。
    /// </summary>
    [Test]
    public async Task RootContainer_Should_ResolveSingletonServicesAsync()
    {
        SampleGeneratedContainer container = new();

        IMviViewRegistry firstRegistry = container.Resolve<IMviViewRegistry>();
        object secondRegistry = container.Resolve(typeof(IMviViewRegistry));

        await Assert.That(ReferenceEquals(firstRegistry, secondRegistry)).IsTrue();
    }

    /// <summary>
    /// 验证同一作用域内 Scoped 服务复用实例，不同作用域创建不同实例。
    /// </summary>
    [Test]
    public async Task Scope_Should_CacheScopedServicesWithinSameScopeAsync()
    {
        SampleGeneratedContainer container = new();

        using IMviScope firstScope = container.CreateScope();
        using IMviScope secondScope = container.CreateScope();

        LoginViewModel firstLogin = firstScope.Resolve<LoginViewModel>();
        LoginViewModel secondLogin = firstScope.Resolve<LoginViewModel>();
        LoginViewModel otherScopeLogin = secondScope.Resolve<LoginViewModel>();

        await Assert.That(ReferenceEquals(firstLogin, secondLogin)).IsTrue();
        await Assert.That(ReferenceEquals(firstLogin, otherScopeLogin)).IsFalse();
    }

    /// <summary>
    /// 验证作用域可以解析登录 Store，并且 Store 与 ViewModel 共享同一作用域实例。
    /// </summary>
    [Test]
    public async Task Scope_Should_ResolveLoginStoreAndViewModelAsync()
    {
        SampleGeneratedContainer container = new();

        using IMviScope scope = container.CreateScope();

        IMviStore<LoginState, LoginIntent, LoginEffect> store =
            scope.Resolve<IMviStore<LoginState, LoginIntent, LoginEffect>>();
        LoginViewModel viewModel = scope.Resolve<LoginViewModel>();

        await Assert.That(store).IsNotNull();
        await Assert.That(viewModel).IsNotNull();
    }

    /// <summary>
    /// 验证生成容器会暴露服务描述和生命周期信息。
    /// </summary>
    [Test]
    public async Task GeneratedContainer_Should_ExposeServiceDescriptorsAsync()
    {
        SampleGeneratedContainer container = new();

        MviServiceDescriptor loginDescriptor = container.ServiceDescriptors
            .Single(static descriptor => descriptor.ServiceType == typeof(LoginViewModel));
        MviServiceDescriptor registryDescriptor = container.ServiceDescriptors
            .Single(static descriptor => descriptor.ServiceType == typeof(IMviViewRegistry));

        await Assert.That(loginDescriptor.Lifetime).IsEqualTo(ServiceLifetime.Scoped);
        await Assert.That(registryDescriptor.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }
}
