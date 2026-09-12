using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页自己的状态，显示名通过中介导航请求传入。
/// </summary>
public sealed record HomeState : IMviState
{
    /// <summary>获取当前用户显示名。</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static HomeState Initial { get; } = new();
}
