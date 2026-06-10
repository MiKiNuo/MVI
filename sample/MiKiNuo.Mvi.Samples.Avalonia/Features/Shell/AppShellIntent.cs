using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳意图。
/// </summary>
public abstract partial record AppShellIntent : IMviIntent
{
    /// <summary>
    /// 表示显示页面的意图。
    /// </summary>
    /// <param name="PageKey">页面键（<see cref="ShellPageKeys"/> 中之一）。</param>
    /// <param name="Title">页面标题。</param>
    public sealed partial record ShowPage(string PageKey, string Title) : AppShellIntent;

    /// <summary>
    /// 创建显示页面意图。
    /// </summary>
    /// <param name="pageKey">页面键。</param>
    /// <param name="title">页面标题。</param>
    /// <returns>显示页面意图。</returns>
    public static ShowPage CreateShowPage(string pageKey, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentNullException.ThrowIfNull(title);
        return new ShowPage(pageKey, title);
    }
}
