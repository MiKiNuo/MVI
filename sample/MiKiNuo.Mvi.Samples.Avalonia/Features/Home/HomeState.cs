using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页状态。主页自身无业务数据，用户显示名经兄弟绑定来自应用壳。
/// </summary>
public sealed record HomeState : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static HomeState Initial { get; } = new();
}
