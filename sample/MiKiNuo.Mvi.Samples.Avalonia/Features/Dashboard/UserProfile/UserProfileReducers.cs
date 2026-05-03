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
    /// 处理修改角色名称意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">修改角色名称意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<UserProfileState, UserProfileEffect> Reduce(
        UserProfileState state,
        UserProfileIntent.ChangeRole intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<UserProfileState, UserProfileEffect>(state with
        {
            RoleName = intent.RoleName
        });
    }
}
