using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅中间件。
/// </summary>
public sealed class LobbyMiddleware : IMviMiddleware<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <inheritdoc />
    public async ValueTask<MviReduceResult<LobbyState, LobbyEffect>> InvokeAsync(
        MviMiddlewareContext<LobbyState, LobbyIntent, LobbyEffect> context,
        MviMiddlewareStep<LobbyState, LobbyIntent, LobbyEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);
        string before = $"Lobby Middleware Before Intent={typeof(LobbyIntent).Name}";
        MviReduceResult<LobbyState, LobbyEffect> result = await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        string after = $"Lobby Middleware After Panel={result.State.CurrentPanel}, Gold={result.State.Gold}, Stamina={result.State.Stamina}, Power={result.State.HeroTeamPower}";
        LobbyState nextState = result.State with
        {
            ActivityLog = AppendLog(result.State.ActivityLog, before, after),
        };
        return MviReduceResult.StateAndEffects<LobbyState, LobbyEffect>(
            nextState,
            [.. result.Effects, new LobbyEffect.Trace(before), new LobbyEffect.Trace(after)]);
    }

    private static string AppendLog(string activityLog, string before, string after)
    {
        ArgumentNullException.ThrowIfNull(activityLog);
        ArgumentException.ThrowIfNullOrWhiteSpace(before);
        ArgumentException.ThrowIfNullOrWhiteSpace(after);
        string timestamp = DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        return string.Concat(activityLog, "[", timestamp, "] ", before, Environment.NewLine, "[", timestamp, "] ", after, Environment.NewLine);
    }
}
