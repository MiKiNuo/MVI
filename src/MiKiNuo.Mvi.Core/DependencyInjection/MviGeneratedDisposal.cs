namespace MiKiNuo.Mvi.Core.DependencyInjection;

/// <summary>
/// 提供源生成依赖注入容器使用的显式释放辅助方法。
/// </summary>
public static class MviGeneratedDisposal
{
    /// <summary>
    /// 按照生成代码提供的实例顺序释放对象。
    /// </summary>
    /// <param name="instances">需要释放的实例集合。</param>
    /// <returns>异步释放任务。</returns>
    public static async ValueTask DisposeAsync(IReadOnlyList<object?> instances)
    {
        for (var index = instances.Count - 1; index >= 0; index--)
        {
            var instance = instances[index];
            if (instance is null)
            {
                continue;
            }

            if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
