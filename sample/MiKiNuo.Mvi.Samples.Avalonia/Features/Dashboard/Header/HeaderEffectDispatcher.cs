using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件副作用分发器。
/// </summary>
public sealed class HeaderEffectDispatcher : IMviEffectDispatcher<HeaderEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(HeaderEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
