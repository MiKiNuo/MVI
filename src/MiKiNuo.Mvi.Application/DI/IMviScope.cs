namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示 MVI 服务作用域。
/// </summary>
public interface IMviScope : IDisposable
{
    /// <summary>
    /// 解析作用域服务。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public TService Resolve<TService>()
        where TService : notnull;
}
