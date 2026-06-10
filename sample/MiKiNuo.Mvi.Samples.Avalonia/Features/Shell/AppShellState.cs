using System;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳 MVI 状态。
/// <para>
/// 不再持有任何页面 <c>*ViewModel</c> 引用；
/// 顶层页面 VM 通过 <see cref="IShellPageFactory"/> 按 <see cref="CurrentPageKey"/> 解析。
/// </para>
/// </summary>
public sealed record AppShellState : IMviState
{
    /// <summary>
    /// 初始化应用壳 MVI 状态。
    /// </summary>
    /// <param name="currentPageKey">当前顶层页面键。</param>
    /// <param name="title">当前页面标题。</param>
    public AppShellState(string currentPageKey, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPageKey);
        ArgumentNullException.ThrowIfNull(title);
        CurrentPageKey = currentPageKey;
        Title = title;
    }

    /// <summary>
    /// 获取当前顶层页面键。
    /// </summary>
    public string CurrentPageKey { get; init; }

    /// <summary>
    /// 获取当前页面标题。
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static AppShellState Initial { get; } = new(
        currentPageKey: ShellPageKeys.Login,
        title: "MiKiNuo MVI");
}
