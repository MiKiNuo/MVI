namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>
/// 表示编译期 DI 服务注册特性。
/// </summary>
/// <param name="lifetime">服务生命周期。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DiServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// 获取服务生命周期。
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// 获取或设置服务接口类型。
    /// </summary>
    public Type? ServiceType { get; set; }
}
