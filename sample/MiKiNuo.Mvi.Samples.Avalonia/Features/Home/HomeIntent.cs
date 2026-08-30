using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页意图。
/// </summary>
public abstract partial record HomeIntent : IMviIntent
{
    /// <summary>
    /// 表示退出登录意图。
    /// </summary>
    public sealed partial record Logout : HomeIntent;
}
