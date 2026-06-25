using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件规约器。
/// </summary>
public sealed partial class UserProfileReducer
    : MviReducerBase<UserProfileState, UserProfileIntent, UserProfileEffect>
{
    /// <summary>
    /// 处理角色变更意图。
    /// </summary>
    [MviReduce(typeof(UserProfileIntent.ChangeRole))]
    private static MviReduceResult<UserProfileState, UserProfileEffect> HandleChangeRole(
        UserProfileState state,
        UserProfileIntent.ChangeRole intent)
    {
        return MviReduceResult.State<UserProfileState, UserProfileEffect>(
            state with { RoleName = intent.RoleName });
    }
}
