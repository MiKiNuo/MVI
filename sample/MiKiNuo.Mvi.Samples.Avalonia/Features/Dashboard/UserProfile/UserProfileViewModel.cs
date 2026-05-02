using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.UserProfile;

/// <summary>
/// 表示用户信息组件 ViewModel。
/// </summary>
public sealed partial class UserProfileViewModel
    : MviViewModelBase<UserProfileState, UserProfileIntent, UserProfileEffect>
{
    /// <summary>
    /// 初始化用户信息组件 ViewModel。
    /// </summary>
    /// <param name="store">用户信息组件状态存储。</param>
    public UserProfileViewModel(IMviStore<UserProfileState, UserProfileIntent, UserProfileEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取显示名称。
    /// </summary>
    [MviBind(nameof(UserProfileState.DisplayName))]
    public partial string DisplayName { get; private set; }

    /// <summary>
    /// 获取角色名称。
    /// </summary>
    [MviBind(nameof(UserProfileState.RoleName))]
    public partial string RoleName { get; private set; }
}
