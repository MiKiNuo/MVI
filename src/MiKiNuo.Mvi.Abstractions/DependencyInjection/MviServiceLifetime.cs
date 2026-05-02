namespace MiKiNuo.Mvi.Abstractions.DependencyInjection;

/// <summary>
/// 表示 MVI 依赖注入服务生命周期。
/// </summary>
public enum MviServiceLifetime
{
    /// <summary>
    /// 每次解析时创建新实例。
    /// </summary>
    Transient = 0,

    /// <summary>
    /// 在同一作用域内复用实例。
    /// </summary>
    Scoped = 1,

    /// <summary>
    /// 在整个应用容器内复用实例。
    /// </summary>
    Singleton = 2,
}
