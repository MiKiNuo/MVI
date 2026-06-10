using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 意图。
/// </summary>
public abstract partial record AppShellIntent : IMviIntent
{
    /// <summary>
    /// 表示显示登录界面的意图。
    /// </summary>
    public sealed partial record ShowLogin : AppShellIntent;

    /// <summary>
    /// 表示显示游戏大厅的意图。
    /// </summary>
    public sealed partial record ShowLobby : AppShellIntent;
}
