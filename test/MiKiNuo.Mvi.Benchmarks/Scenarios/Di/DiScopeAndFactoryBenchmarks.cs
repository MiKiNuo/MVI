using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Benchmarks.Composition;
using MiKiNuo.Mvi.Domain.DI;
using ServiceLifetime = MiKiNuo.Mvi.Domain.DI.ServiceLifetime;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Di;

/// <summary>
/// 表示 DI 作用域与工厂方法基准：
/// CreateScope 创建/销毁、作用域内解析（缓存命中与瞬态）、
/// 泛型 Resolve 与 CreateWith 工厂路径。
/// </summary>
[MemoryDiagnoser]
public class DiScopeAndFactoryBenchmarks : IDisposable
{
    private GeneratedMviContainer _container = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _msDiScope = null!;
    private IMviScope _generatedScope = null!;
    private Type _scopedServiceType = null!;
    private Type _transientServiceType = null!;

    /// <summary>
    /// 构建容器、服务提供器与作用域，并挑选链中位置的作用域/瞬态服务类型。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _container = new GeneratedMviContainer();
        _provider = MsDiContainerFactory.Build(_container.ServiceDescriptors);
        _scopedServiceType = SyntheticDiServiceCatalog.PickDescriptor(
            _container.ServiceDescriptors, DiScanPosition.Middle, ServiceLifetime.Scoped).ServiceType;
        _transientServiceType = SyntheticDiServiceCatalog.PickDescriptor(
            _container.ServiceDescriptors, DiScanPosition.Middle, ServiceLifetime.Transient).ServiceType;

        _generatedScope = _container.CreateScope();
        _msDiScope = _provider.CreateScope();

        // 预热作用域缓存，基准测量稳态。
        _ = _generatedScope.Resolve(_scopedServiceType);
        _ = _msDiScope.ServiceProvider.GetRequiredService(_scopedServiceType);
    }

    /// <summary>
    /// 清理作用域与服务提供器。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放作用域与服务提供器资源。
    /// </summary>
    public void Dispose()
    {
        _generatedScope?.Dispose();
        _msDiScope?.Dispose();
        _provider?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 生成容器创建作用域：仅分配作用域对象与缓存字典。
    /// </summary>
    /// <returns>新创建的作用域。</returns>
    [Benchmark]
    public IMviScope GeneratedCreateScope()
    {
        return _container.CreateScope();
    }

    /// <summary>
    /// MS.DI 创建作用域。
    /// </summary>
    /// <returns>新创建的作用域。</returns>
    [Benchmark]
    public IServiceScope MsDiCreateScope()
    {
        return _provider.CreateScope();
    }

    /// <summary>
    /// 生成容器作用域内解析作用域服务：首次构造后命中作用域缓存字典。
    /// </summary>
    /// <returns>作用域内缓存的服务实例。</returns>
    [Benchmark]
    public object GeneratedScopeResolveScoped()
    {
        return _generatedScope.Resolve(_scopedServiceType);
    }

    /// <summary>
    /// 生成容器作用域内解析瞬态服务：每次都走 if-else 链并重新构造。
    /// </summary>
    /// <returns>新构造的服务实例。</returns>
    [Benchmark]
    public object GeneratedScopeResolveTransient()
    {
        return _generatedScope.Resolve(_transientServiceType);
    }

    /// <summary>
    /// MS.DI 作用域内解析作用域服务。
    /// </summary>
    /// <returns>作用域内缓存的服务实例。</returns>
    [Benchmark]
    public object MsDiScopeResolveScoped()
    {
        return _msDiScope.ServiceProvider.GetRequiredService(_scopedServiceType);
    }

    /// <summary>
    /// MS.DI 作用域内解析瞬态服务。
    /// </summary>
    /// <returns>新构造的服务实例。</returns>
    [Benchmark]
    public object MsDiScopeResolveTransient()
    {
        return _msDiScope.ServiceProvider.GetRequiredService(_transientServiceType);
    }

    /// <summary>
    /// 生成容器泛型解析链中单例：泛型入口转调非泛型路径。
    /// </summary>
    /// <returns>单例服务实例。</returns>
    [Benchmark]
    public SyntheticService000 GeneratedResolveGenericSingleton()
    {
        return _container.Resolve<SyntheticService000>();
    }

    /// <summary>
    /// 生成容器泛型解析链中瞬态：每次转调非泛型路径并重新构造。
    /// </summary>
    /// <returns>新构造的服务实例。</returns>
    [Benchmark]
    public SyntheticService001 GeneratedResolveGenericTransient()
    {
        return _container.Resolve<SyntheticService001>();
    }

    /// <summary>
    /// 生成容器 CreateWith 零参工厂：走零参 if-else 分支构造新实例。
    /// </summary>
    /// <returns>新构造的服务实例。</returns>
    [Benchmark]
    public SyntheticService001 GeneratedCreateWithZeroArgs()
    {
        return _container.CreateWith<SyntheticService001>();
    }
}
