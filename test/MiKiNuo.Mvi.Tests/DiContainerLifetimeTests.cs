using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 [MviFeature]/[MviComposition] 生成容器的生命周期与装配回归测试。
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
    /// 验证组合构建器一次创建全部成员实例，实例身份互不重复。
    /// </summary>
    [Test]
    public async Task Composition_Should_AssembleAllMemberInstancesAsync()
    {
        GeneratedMviContainer container = new();

        AppComposition composition = await container.CreateAppCompositionAsync();

        await Assert.That(composition.AppShell).IsNotNull();
        await Assert.That(composition.Login).IsNotNull();
        await Assert.That(composition.Register).IsNotNull();
        await Assert.That(composition.ResetPassword).IsNotNull();
        await Assert.That(composition.Home).IsNotNull();

        Guid[] ids =
        [
            composition.AppShell.Id,
            composition.Login.Id,
            composition.Register.Id,
            composition.ResetPassword.Id,
            composition.Home.Id,
        ];
        await Assert.That(ids.Distinct().Count()).IsEqualTo(5);

        await composition.DisposeAsync();
    }

    /// <summary>
    /// 验证实例工厂每次创建独立的 Feature 实例。
    /// </summary>
    [Test]
    public async Task InstanceFactory_Should_CreateIndependentInstancesAsync()
    {
        GeneratedMviContainer container = new();
        using MiKiNuo.Mvi.Application.MVI.Mediator.MviCompositionScope scope = new();

        MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<MiKiNuo.Mvi.Samples.Avalonia.Features.Login.LoginViewModel> first =
            await container.CreateLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));
        MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<MiKiNuo.Mvi.Samples.Avalonia.Features.Login.LoginViewModel> second =
            await container.CreateLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));

        await Assert.That(ReferenceEquals(first, second)).IsFalse();
        await Assert.That(ReferenceEquals(first.ViewModel, second.ViewModel)).IsFalse();
        await Assert.That(first.Id).IsNotEqualTo(second.Id);

        await first.DisposeAsync();
        await second.DisposeAsync();
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
