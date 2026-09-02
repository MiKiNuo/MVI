using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Benchmarks.Composition;
using MiKiNuo.Mvi.Domain.DI;
using ServiceLifetime = MiKiNuo.Mvi.Domain.DI.ServiceLifetime;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Di;

/// <summary>
/// 表示 DI 启动成本基准：容器构造与全量单例预热。
/// 生成容器构造仅初始化静态描述符数组；MS.DI 需要在构建期编译解析管线。
/// </summary>
[MemoryDiagnoser]
public class DiStartupBenchmarks
{
    /// <summary>
    /// 生成容器纯构造：理论上限（仅静态描述符数组，不实例化任何服务）。
    /// </summary>
    /// <returns>新构造的生成容器。</returns>
    [Benchmark(Baseline = true)]
    public GeneratedMviContainer NewContainerOnly()
    {
        return new GeneratedMviContainer();
    }

    /// <summary>
    /// 生成容器构造并预热全部单例：模拟应用启动完成态。
    /// </summary>
    /// <returns>预热完成的生成容器。</returns>
    [Benchmark]
    public GeneratedMviContainer NewContainerAndWarmupSingletons()
    {
        GeneratedMviContainer container = new();
        foreach (MviServiceDescriptor descriptor in container.ServiceDescriptors)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _ = container.Resolve(descriptor.ServiceType);
            }
        }

        return container;
    }

    /// <summary>
    /// MS.DI 构建服务提供器并预热全部单例：行业参照的启动成本。
    /// </summary>
    /// <returns>预热完成的 MS.DI 服务提供器。</returns>
    [Benchmark]
    public ServiceProvider MsDiBuildProviderAndWarmupSingletons()
    {
        GeneratedMviContainer container = new();
        ServiceProvider provider = MsDiContainerFactory.Build(container.ServiceDescriptors);
        foreach (MviServiceDescriptor descriptor in container.ServiceDescriptors)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _ = provider.GetRequiredService(descriptor.ServiceType);
            }
        }

        return provider;
    }
}
