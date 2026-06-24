using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件变更规约器。
/// </summary>
public sealed partial class UserProfileMutationReducer
    : MviMutationReducerBase<UserProfileState, UserProfileMutation, UserProfileEffect>
{
    /// <summary>
    /// 应用设置角色名称变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<UserProfileState, UserProfileEffect> HandleSetRoleName(
        UserProfileState state,
        UserProfileMutation.SetRoleName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<UserProfileState, UserProfileEffect>(state.Apply(mutation));
    }
}
