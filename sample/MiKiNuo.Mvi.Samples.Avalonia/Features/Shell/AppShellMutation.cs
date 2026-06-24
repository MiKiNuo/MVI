using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳变更。
/// </summary>
public abstract record AppShellMutation : IMviMutation<AppShellState>
{
    /// <summary>
    /// 表示设置当前页面键的变更。
    /// </summary>
    /// <param name="Value">页面键。</param>
    [MviMutation(Path = "CurrentPageKey")]
    public sealed record SetCurrentPageKey(string Value) : AppShellMutation;

    /// <summary>
    /// 表示设置页面标题的变更。
    /// </summary>
    /// <param name="Value">页面标题。</param>
    [MviMutation(Path = "Title")]
    public sealed record SetTitle(string Value) : AppShellMutation;
}
