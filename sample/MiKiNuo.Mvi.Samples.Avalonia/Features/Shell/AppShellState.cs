namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>壳只保存当前页面和公开显示信息，不持有认证凭据。</summary>
/// <param name="CurrentPage">当前页面。</param>
/// <param name="DisplayName">显示名。</param>
public sealed record AppShellState(ShellPage CurrentPage = ShellPage.Login, string? DisplayName = null) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static AppShellState Initial { get; } = new();
}
