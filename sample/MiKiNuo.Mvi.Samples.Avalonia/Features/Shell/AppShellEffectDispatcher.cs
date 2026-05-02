using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳副作用分发器。
/// </summary>
public sealed class AppShellEffectDispatcher : IMviEffectDispatcher<AppShellEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(AppShellEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
