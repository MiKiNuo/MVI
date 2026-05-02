using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="CurrentViewModel">当前视图模型。</param>
public sealed record AppShellState(string Title, object? CurrentViewModel) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static AppShellState Initial { get; } = new("MiKiNuo MVI", null);
}
