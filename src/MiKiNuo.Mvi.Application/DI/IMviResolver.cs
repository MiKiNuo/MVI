namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示 MVI 服务解析器。
/// </summary>
public interface IMviResolver
{
    /// <summary>
    /// 解析服务。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public TService Resolve<TService>()
        where TService : notnull;

    /// <summary>
    /// 创建作用域。
    /// </summary>
    /// <returns>服务作用域。</returns>
    public IMviScope CreateScope();
}
