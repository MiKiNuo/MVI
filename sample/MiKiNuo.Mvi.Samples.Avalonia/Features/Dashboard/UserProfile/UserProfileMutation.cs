using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件变更。
/// </summary>
public abstract record UserProfileMutation : IMviMutation<UserProfileState>
{
    /// <summary>
    /// 表示设置角色名称的变更。
    /// </summary>
    /// <param name="Value">角色名称。</param>
    [MviMutation(Path = "RoleName")]
    public sealed record SetRoleName(string Value) : UserProfileMutation;
}
