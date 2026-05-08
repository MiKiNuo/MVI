using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳副作用分发器。
/// </summary>
public sealed class AppShellEffectDispatcher : IMviEffectDispatcher<AppShellEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(AppShellEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();
        if (effect is AppShellEffect.Trace trace)
        {
            GD.Print($"[Godot Game MVI Shell Effect] {trace.Text}");
        }

        return ValueTask.CompletedTask;
    }
}
