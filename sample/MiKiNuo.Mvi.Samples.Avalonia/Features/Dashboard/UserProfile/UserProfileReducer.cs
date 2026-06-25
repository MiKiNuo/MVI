using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件规约器。
/// </summary>
public sealed class UserProfileReducer
    : MviReducerBase<UserProfileState, UserProfileIntent, UserProfileEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<UserProfileState, UserProfileEffect> Reduce(
        UserProfileState state,
        UserProfileIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            UserProfileIntent.ChangeRole changeRole => MviReduceResult.State<UserProfileState, UserProfileEffect>(
                state with { RoleName = changeRole.RoleName }),
            _ => MviReduceResult.State<UserProfileState, UserProfileEffect>(state),
        };
    }
}
