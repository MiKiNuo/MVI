using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示编译期 DI 生成器输出的服务描述。
/// </summary>
public sealed class MviServiceDescriptor
{
    /// <summary>
    /// 初始化 MVI 服务描述。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    /// <param name="implementationType">实现类型。</param>
    /// <param name="lifetime">服务生命周期。</param>
    public MviServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);

        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }

    /// <summary>
    /// 获取服务类型。
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// 获取实现类型。
    /// </summary>
    public Type ImplementationType { get; }

    /// <summary>
    /// 获取服务生命周期。
    /// </summary>
    public ServiceLifetime Lifetime { get; }
}
