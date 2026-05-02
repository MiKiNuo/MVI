namespace MiKiNuo.Mvi.Abstractions.DependencyInjection;

/// <summary>
/// 标记当前类型由源生成依赖注入容器管理。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MviServiceAttribute : Attribute
{
    /// <summary>
    /// 初始化服务注册特性。
    /// </summary>
    /// <param name="serviceType">服务抽象类型。</param>
    /// <param name="lifetime">服务生命周期。</param>
    public MviServiceAttribute(Type serviceType, MviServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }

    /// <summary>
    /// 获取服务抽象类型。
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// 获取服务生命周期。
    /// </summary>
    public MviServiceLifetime Lifetime { get; }
}
