namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>大厅状态。</summary>
/// <param name="DisplayName">公开显示名。</param>
public sealed record HomeState(string DisplayName = "") : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static HomeState Initial { get; } = new();
}
