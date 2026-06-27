using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件规约器。
/// </summary>
public sealed partial class UserProfileReducer
    : MviReducerBase<UserProfileState, UserProfileIntent, UnitEffect>
{
    /// <summary>
    /// 处理角色变更意图。
    /// </summary>
    [MviReduce(typeof(UserProfileIntent.ChangeRole))]
    private MviReduceResult<UserProfileState, UnitEffect> HandleChangeRole(
        UserProfileState state,
        UserProfileIntent.ChangeRole intent)
    {
        return MviReduceResult.State<UserProfileState, UnitEffect>(
            state with { RoleName = intent.RoleName });
    }
}
