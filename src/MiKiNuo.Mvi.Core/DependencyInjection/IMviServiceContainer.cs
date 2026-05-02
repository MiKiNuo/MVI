namespace MiKiNuo.Mvi.Core.DependencyInjection;

/// <summary>
/// 表示由源生成器生成的 MVI 服务容器。
/// </summary>
public interface IMviServiceContainer : IAsyncDisposable
{
    /// <summary>
    /// 创建新的 MVI 服务作用域。
    /// </summary>
    /// <returns>服务作用域。</returns>
    IMviServiceScope CreateScope();
}
