using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件副作用分发器。
/// </summary>
public sealed class UserProfileEffectDispatcher : IMviEffectDispatcher<UserProfileEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(UserProfileEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
