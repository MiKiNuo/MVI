using Avalonia.Threading;
using MiKiNuo.Mvi.Application.MVI.Threading;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Threading;

/// <summary>
/// 表示 Avalonia 平台 UI 调度器。
/// </summary>
public sealed class AvaloniaMviUiDispatcher : IMviUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Dispatcher.UIThread.Post(action);
    }
}
