namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>组件通过中介者提交的页面导航请求。</summary>
/// <param name="Page">目标页。</param>
/// <param name="DisplayName">仅在登录成功后提供公开显示名。</param>
public sealed record NavigateToPageRequest(ShellPage Page, string? DisplayName = null) : IMviRequest<bool>;
