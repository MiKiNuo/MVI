namespace MiKiNuo.Mvi.Domain.DI;

/// <summary>
/// 表示服务生命周期。
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// 单例生命周期。
    /// </summary>
    Singleton,

    /// <summary>
    /// 作用域生命周期。
    /// </summary>
    Scoped,

    /// <summary>
    /// 瞬态生命周期。
    /// </summary>
    Transient
}
