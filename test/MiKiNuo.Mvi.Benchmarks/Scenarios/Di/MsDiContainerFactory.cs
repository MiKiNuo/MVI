using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Domain.DI;
using MviServiceLifetime = MiKiNuo.Mvi.Domain.DI.ServiceLifetime;
using MsDiServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Di;

/// <summary>
/// 表示 MS.DI 基线工厂：把生成容器的服务描述符等价翻译为
/// Microsoft.Extensions.DependencyInjection 注册，作为行业参照基线。
/// </summary>
public static class MsDiContainerFactory
{
    /// <summary>
    /// 从生成容器的服务描述符列表构建 MS.DI 服务提供器。
    /// </summary>
    /// <param name="descriptors">生成容器暴露的服务描述符列表。</param>
    /// <returns>构建完成的 MS.DI 服务提供器。</returns>
    public static ServiceProvider Build(IReadOnlyList<MviServiceDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        ServiceCollection services = new();
        foreach (MviServiceDescriptor descriptor in descriptors)
        {
            services.Add(new ServiceDescriptor(
                descriptor.ServiceType,
                descriptor.ImplementationType,
                MapLifetime(descriptor.Lifetime)));
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 把 MVI 服务生命周期映射为 MS.DI 服务生命周期。
    /// </summary>
    /// <param name="lifetime">MVI 服务生命周期。</param>
    /// <returns>等价的 MS.DI 服务生命周期。</returns>
    private static MsDiServiceLifetime MapLifetime(MviServiceLifetime lifetime)
    {
        return lifetime switch
        {
            MviServiceLifetime.Singleton => MsDiServiceLifetime.Singleton,
            MviServiceLifetime.Scoped => MsDiServiceLifetime.Scoped,
            _ => MsDiServiceLifetime.Transient,
        };
    }
}
