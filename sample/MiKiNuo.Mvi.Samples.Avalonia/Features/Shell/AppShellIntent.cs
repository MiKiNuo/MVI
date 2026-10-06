namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>应用壳接收的导航意图。</summary>
/// <param name="Page">页面。</param>
/// <param name="DisplayName">公开显示名。</param>
public sealed record AppShellIntent(ShellPage Page, string? DisplayName = null) : IMviIntent;
