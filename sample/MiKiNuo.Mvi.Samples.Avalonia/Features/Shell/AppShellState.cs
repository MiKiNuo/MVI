using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳状态。
/// </summary>
/// <param name="CurrentPage">当前页面。</param>
/// <param name="DisplayName">已登录用户显示名。</param>
public sealed record AppShellState(
    ShellPage CurrentPage,
    string? DisplayName) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static AppShellState Initial { get; } = new(ShellPage.Login, null);
}
