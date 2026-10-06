namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>进入大厅的范围内事实通知。</summary>
/// <param name="DisplayName">公开显示名。</param>
public sealed record HomeEnteredNotification(string DisplayName) : IMviNotification;
