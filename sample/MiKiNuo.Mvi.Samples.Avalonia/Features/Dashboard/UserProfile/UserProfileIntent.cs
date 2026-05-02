using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件意图。
/// </summary>
public abstract partial record UserProfileIntent : IMviIntent
{
    /// <summary>
    /// 表示修改角色名称意图。
    /// </summary>
    /// <param name="RoleName">角色名称。</param>
    public sealed partial record ChangeRole(string RoleName) : UserProfileIntent;
}
