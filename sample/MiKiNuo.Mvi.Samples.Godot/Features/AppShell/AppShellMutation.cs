using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 MVI 变更。
/// </summary>
public abstract record AppShellMutation : IMviMutation<AppShellState>
{
    /// <summary>
    /// 表示设置当前页面键的变更。
    /// </summary>
    /// <param name="Value">页面键。</param>
    [MviMutation(Path = "CurrentScreen")]
    public sealed record SetCurrentScreen(string Value) : AppShellMutation;

    /// <summary>
    /// 表示设置当前页面标题的变更。
    /// </summary>
    /// <param name="Value">页面标题。</param>
    [MviMutation(Path = "CurrentTitle")]
    public sealed record SetCurrentTitle(string Value) : AppShellMutation;

    /// <summary>
    /// 表示设置应用壳提示消息的变更。
    /// </summary>
    /// <param name="Value">提示消息。</param>
    [MviMutation(Path = "ShellMessage")]
    public sealed record SetShellMessage(string Value) : AppShellMutation;
}
