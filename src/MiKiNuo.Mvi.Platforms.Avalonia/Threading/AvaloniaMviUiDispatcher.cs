using Avalonia.Threading;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Threading;

/// <summary>
/// 表示 Avalonia 平台 UI 调度器。
/// </summary>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IMviUiDispatcher))]
public sealed class AvaloniaMviUiDispatcher : IMviUiDispatcher
{
    /// <summary>
    /// 将操作投递到 UI 线程。
    /// </summary>
    /// <param name="action">需要在 UI 线程上执行的操作。</param>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Dispatcher.UIThread.Post(action);
    }
}
