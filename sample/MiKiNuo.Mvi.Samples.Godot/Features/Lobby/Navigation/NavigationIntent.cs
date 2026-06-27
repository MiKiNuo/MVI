using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅导航 MVI 意图。
/// </summary>
public abstract partial record NavigationIntent : IMviIntent
{
    /// <summary>表示选择面板的意图。</summary>
    /// <param name="Panel">目标面板。</param>
    public sealed partial record SelectPanel(LobbyPanel Panel) : NavigationIntent;

    /// <summary>表示退出登录的意图。</summary>
    public sealed partial record Logout : NavigationIntent;
}
