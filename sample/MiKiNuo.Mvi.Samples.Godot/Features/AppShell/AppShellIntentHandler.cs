using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳意图处理器。
/// </summary>
public sealed class AppShellIntentHandler
    : IMviIntentHandler<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<AppShellEffect>> HandleAsync(
        AppShellState state,
        AppShellIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<AppShellEffect> effects = intent switch
        {
            AppShellIntent.ShowLogin => new AppShellEffect[] { new AppShellEffect.Trace("Shell ShowLogin") },
            AppShellIntent.ShowLobby => new AppShellEffect[] { new AppShellEffect.Trace("Shell ShowLobby") },
            _ => Array.Empty<AppShellEffect>(),
        };
        return new ValueTask<IReadOnlyList<AppShellEffect>>(effects);
    }
}
