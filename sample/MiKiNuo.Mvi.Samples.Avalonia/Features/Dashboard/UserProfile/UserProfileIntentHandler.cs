using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件意图处理器。
/// </summary>
public sealed class UserProfileIntentHandler
    : IMviIntentHandler<UserProfileState, UserProfileIntent, UserProfileMutation, UserProfileEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<UserProfileMutation, UserProfileEffect>> HandleAsync(
        UserProfileState state,
        UserProfileIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<UserProfileMutation, UserProfileEffect> result = intent switch
        {
            UserProfileIntent.ChangeRole changeRole => HandleChangeRole(changeRole),
            _ => MviHandleResult.Empty<UserProfileMutation, UserProfileEffect>(),
        };
        return new ValueTask<MviHandleResult<UserProfileMutation, UserProfileEffect>>(result);
    }

    private static MviHandleResult<UserProfileMutation, UserProfileEffect> HandleChangeRole(
        UserProfileIntent.ChangeRole intent)
    {
        return MviHandleResult.Mutations<UserProfileMutation, UserProfileEffect>(
            new UserProfileMutation.SetRoleName(intent.RoleName));
    }
}
