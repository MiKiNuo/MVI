namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>应用壳的页面变化。</summary>
/// <param name="Page">目标页。</param>
/// <param name="DisplayName">显示名。</param>
public sealed record AppShellMutation(ShellPage Page, string? DisplayName) : IMviMutation<AppShellState>;
