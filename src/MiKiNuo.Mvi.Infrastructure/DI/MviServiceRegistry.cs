namespace MiKiNuo.Mvi.Infrastructure.DI;

/// <summary>
/// 表示手写组合根可复用的服务注册表。
/// </summary>
public sealed class MviServiceRegistry
{
    private readonly Dictionary<Type, Func<object>> _factories;

    /// <summary>
    /// 初始化服务注册表。
    /// </summary>
    public MviServiceRegistry()
    {
        _factories = new Dictionary<Type, Func<object>>();
    }

    /// <summary>
    /// 注册服务工厂。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <param name="factory">服务工厂。</param>
    public void Add<TService>(Func<TService> factory)
        where TService : notnull
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _factories[typeof(TService)] = () => factory();
    }

    /// <summary>
    /// 解析服务。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public TService Resolve<TService>()
        where TService : notnull
    {
        if (_factories.TryGetValue(typeof(TService), out Func<object>? factory))
        {
            return (TService)factory();
        }

        throw new DiServiceNotFoundException(typeof(TService));
    }
}
