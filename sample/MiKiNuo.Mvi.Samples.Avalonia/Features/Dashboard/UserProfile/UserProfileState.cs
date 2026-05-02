using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件状态。
/// </summary>
/// <param name="DisplayName">显示名称。</param>
/// <param name="RoleName">角色名称。</param>
public sealed record UserProfileState(string DisplayName, string RoleName) : IMviState;
