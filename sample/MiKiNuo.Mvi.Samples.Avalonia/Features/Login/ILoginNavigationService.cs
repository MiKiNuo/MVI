namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录导航服务。
/// </summary>
public interface ILoginNavigationService
{
    /// <summary>
    /// 导航到 Dashboard。
    /// </summary>
    /// <param name="displayName">显示名称。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步导航过程的任务。</returns>
    public ValueTask NavigateToDashboardAsync(string displayName, CancellationToken cancellationToken = default);
}
