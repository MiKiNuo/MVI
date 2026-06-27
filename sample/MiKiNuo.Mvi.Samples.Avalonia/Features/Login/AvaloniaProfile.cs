using MiKiNuo.Mvi.Samples.Shared.Features.Login;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示 Avalonia 示例的登录用户资料。
/// </summary>
/// <param name="DisplayName">显示名称。</param>
public sealed record AvaloniaProfile(string DisplayName) : ILoginProfile;
