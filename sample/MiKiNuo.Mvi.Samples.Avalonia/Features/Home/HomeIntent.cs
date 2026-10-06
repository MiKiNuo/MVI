namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅输入。</summary>
public abstract record HomeIntent : IMviIntent
{
    /// <summary>接收组合范围通知中的公开显示名。</summary>
    /// <param name="DisplayName">显示名。</param>
    public sealed record ShowUser(string DisplayName) : HomeIntent;
    /// <summary>离开大厅。</summary>
    public sealed record Logout : HomeIntent;
}
