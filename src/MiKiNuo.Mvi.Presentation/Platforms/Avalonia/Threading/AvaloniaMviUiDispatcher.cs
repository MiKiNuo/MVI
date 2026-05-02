using Avalonia.Threading;

namespace MiKiNuo.Mvi.Presentation.Platforms.Avalonia.Threading;

/// <summary>
/// 表示 Avalonia UI 调度器。
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
