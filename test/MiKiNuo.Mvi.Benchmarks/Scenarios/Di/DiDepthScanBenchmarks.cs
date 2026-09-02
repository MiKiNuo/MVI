using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Benchmarks.Composition;
using MiKiNuo.Mvi.Domain.DI;
using ServiceLifetime = MiKiNuo.Mvi.Domain.DI.ServiceLifetime;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Di;

/// <summary>
/// 表示 DI 解析链深度扫描基准：
/// 同一份 300+ 服务注册下，比较手写 new（理论上限）、
/// MS.DI（行业参照）与生成容器（if-else 链）在链首/链中/链尾三种深度、
/// 三种生命周期下的稳态解析成本。
/// </summary>
[MemoryDiagnoser]
public class DiDepthScanBenchmarks : IDisposable
{
    private GeneratedMviContainer _container = null!;
    private ServiceProvider _provider = null!;
    private Type _serviceType = null!;

    /// <summary>
    /// 获取或设置解析链深度位置。
    /// </summary>
    [Params(DiScanPosition.First, DiScanPosition.Middle, DiScanPosition.Last)]
    public DiScanPosition Position { get; set; }

    /// <summary>
    /// 获取或设置目标服务生命周期。
    /// </summary>
    [Params(ServiceLifetime.Singleton, ServiceLifetime.Transient, ServiceLifetime.Scoped)]
    public ServiceLifetime Lifetime { get; set; }

    /// <summary>
    /// 构建两个容器并按参数挑选目标服务类型，预热单例以测量稳态解析。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _container = new GeneratedMviContainer();
        _provider = MsDiContainerFactory.Build(_container.ServiceDescriptors);
        _serviceType = SyntheticDiServiceCatalog.PickDescriptor(
            _container.ServiceDescriptors, Position, Lifetime).ServiceType;

        // 预热：单例首次解析包含构造成本，基准只测量稳态。
        _ = _container.Resolve(_serviceType);
        _ = _provider.GetRequiredService(_serviceType);
    }

    /// <summary>
    /// 清理 MS.DI 服务提供器。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放 MS.DI 服务提供器资源。
    /// </summary>
    public void Dispose()
    {
        _provider?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 手写 new 基线：解析的物理上限。
    /// </summary>
    /// <returns>新构造的服务实例。</returns>
    [Benchmark(Baseline = true)]
    public SyntheticService001 NewTransient()
    {
        return new SyntheticService001();
    }

    /// <summary>
    /// 生成容器按类型解析：单例走字典缓存，其余按 if-else 链深度付费。
    /// </summary>
    /// <returns>解析出的服务实例。</returns>
    [Benchmark]
    public object GeneratedContainerResolve()
    {
        return _container.Resolve(_serviceType);
    }

    /// <summary>
    /// MS.DI 按类型解析：哈希查表，理论上与注册深度无关。
    /// </summary>
    /// <returns>解析出的服务实例。</returns>
    [Benchmark]
    public object MsDiResolve()
    {
        return _provider.GetRequiredService(_serviceType);
    }
}
