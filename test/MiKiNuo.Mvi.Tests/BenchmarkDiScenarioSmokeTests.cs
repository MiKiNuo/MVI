using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Benchmarks.Composition;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Di;
using MiKiNuo.Mvi.Domain.DI;
using ServiceLifetime = MiKiNuo.Mvi.Domain.DI.ServiceLifetime;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示基准项目 DI 场景的冒烟测试：
/// 验证深度扫描目录、生成容器与 MS.DI 基线工厂在测量前真实可用。
/// </summary>
public sealed class BenchmarkDiScenarioSmokeTests
{
    /// <summary>
    /// 验证生成容器注册了 300 个合成服务，为深度扫描提供链深度。
    /// </summary>
    [Test]
    public async Task Container_Should_RegisterThreeHundredSyntheticServicesAsync()
    {
        GeneratedMviContainer container = new();

        int syntheticCount = 0;
        foreach (MviServiceDescriptor descriptor in container.ServiceDescriptors)
        {
            if (SyntheticDiServiceCatalog.IsSyntheticService(descriptor))
            {
                syntheticCount++;
            }
        }

        await Assert.That(syntheticCount).IsEqualTo(SyntheticDiServiceCatalog.SyntheticServiceCount);
    }

    /// <summary>
    /// 验证链首/链中/链尾 × 三种生命周期均可从生成容器解析出正确实现类型。
    /// </summary>
    [Test]
    public async Task Container_Should_ResolveAllDepthPositions_ForAllLifetimesAsync()
    {
        GeneratedMviContainer container = new();
        DiScanPosition[] positions = Enum.GetValues<DiScanPosition>();
        ServiceLifetime[] lifetimes = Enum.GetValues<ServiceLifetime>();

        foreach (DiScanPosition position in positions)
        {
            foreach (ServiceLifetime lifetime in lifetimes)
            {
                MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
                    container.ServiceDescriptors, position, lifetime);

                object instance = container.Resolve(descriptor.ServiceType);

                await Assert.That(instance).IsNotNull();
                await Assert.That(instance.GetType()).IsEqualTo(descriptor.ImplementationType);
            }
        }
    }

    /// <summary>
    /// 验证链首单例两次解析返回同一实例（字典缓存命中路径）。
    /// </summary>
    [Test]
    public async Task Container_SingletonAtFirst_Should_ReturnSameInstanceAsync()
    {
        GeneratedMviContainer container = new();
        MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
            container.ServiceDescriptors, DiScanPosition.First, ServiceLifetime.Singleton);

        object first = container.Resolve(descriptor.ServiceType);
        object second = container.Resolve(descriptor.ServiceType);

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    /// <summary>
    /// 验证链尾瞬态两次解析返回不同实例（每次都重新构造）。
    /// </summary>
    [Test]
    public async Task Container_TransientAtLast_Should_ReturnNewInstanceEachTimeAsync()
    {
        GeneratedMviContainer container = new();
        MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
            container.ServiceDescriptors, DiScanPosition.Last, ServiceLifetime.Transient);

        object first = container.Resolve(descriptor.ServiceType);
        object second = container.Resolve(descriptor.ServiceType);

        await Assert.That(ReferenceEquals(first, second)).IsFalse();
    }

    /// <summary>
    /// 验证作用域服务在同一 Scope 内缓存、跨 Scope 各自构造。
    /// </summary>
    [Test]
    public async Task Container_ScopedService_Should_CacheWithinScope_And_VaryAcrossScopesAsync()
    {
        GeneratedMviContainer container = new();
        MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
            container.ServiceDescriptors, DiScanPosition.Middle, ServiceLifetime.Scoped);

        using IMviScope firstScope = container.CreateScope();
        using IMviScope secondScope = container.CreateScope();

        object firstInScope = firstScope.Resolve(descriptor.ServiceType);
        object secondInScope = firstScope.Resolve(descriptor.ServiceType);
        object otherScope = secondScope.Resolve(descriptor.ServiceType);

        await Assert.That(ReferenceEquals(firstInScope, secondInScope)).IsTrue();
        await Assert.That(ReferenceEquals(firstInScope, otherScope)).IsFalse();
    }

    /// <summary>
    /// 验证 CreateWith 零参构造每次都返回新实例（不走单例缓存）。
    /// </summary>
    [Test]
    public async Task Container_CreateWithZeroArgs_Should_ConstructNewInstanceEachTimeAsync()
    {
        GeneratedMviContainer container = new();

        SyntheticService001 first = container.CreateWith<SyntheticService001>();
        SyntheticService001 second = container.CreateWith<SyntheticService001>();

        await Assert.That(first).IsNotNull();
        await Assert.That(ReferenceEquals(first, second)).IsFalse();
    }

    /// <summary>
    /// 验证 MS.DI 基线工厂能从同一份描述符构建 Provider 并解析全部深度位置。
    /// </summary>
    [Test]
    public async Task MsDiFactory_Should_BuildProvider_And_ResolveAllDepthPositionsAsync()
    {
        GeneratedMviContainer container = new();
        using ServiceProvider provider = MsDiContainerFactory.Build(container.ServiceDescriptors);

        foreach (DiScanPosition position in Enum.GetValues<DiScanPosition>())
        {
            foreach (ServiceLifetime lifetime in Enum.GetValues<ServiceLifetime>())
            {
                MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
                    container.ServiceDescriptors, position, lifetime);

                object instance = provider.GetRequiredService(descriptor.ServiceType);

                await Assert.That(instance).IsNotNull();
            }
        }
    }

    /// <summary>
    /// 验证 MS.DI 基线的单例语义与生成容器一致。
    /// </summary>
    [Test]
    public async Task MsDiFactory_Singleton_Should_ReturnSameInstanceAsync()
    {
        GeneratedMviContainer container = new();
        MviServiceDescriptor descriptor = SyntheticDiServiceCatalog.PickDescriptor(
            container.ServiceDescriptors, DiScanPosition.First, ServiceLifetime.Singleton);
        using ServiceProvider provider = MsDiContainerFactory.Build(container.ServiceDescriptors);

        object first = provider.GetRequiredService(descriptor.ServiceType);
        object second = provider.GetRequiredService(descriptor.ServiceType);

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }
}
