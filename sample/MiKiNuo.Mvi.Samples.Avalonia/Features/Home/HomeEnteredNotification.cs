using MiKiNuo.Mvi.Domain.MVI.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示用户已进入主页的事实通知，由应用壳在导航到主页后发布。
/// </summary>
/// <param name="DisplayName">用户显示名。</param>
public sealed record HomeEnteredNotification(string DisplayName) : IMviNotification;
