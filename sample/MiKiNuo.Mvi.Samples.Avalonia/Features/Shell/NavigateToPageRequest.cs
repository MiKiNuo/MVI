using MiKiNuo.Mvi.Domain.MVI.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示跨 Feature 页面导航请求，经 Mediator 路由到应用壳。
/// </summary>
/// <param name="Page">目标页面。</param>
/// <param name="DisplayName">进入主页时携带的用户显示名。</param>
public sealed record NavigateToPageRequest(ShellPage Page, string? DisplayName)
    : IMviRequest<bool>;
