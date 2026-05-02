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
    /// <param name="Title">标题。</param>
    /// <param name="ViewModel">页面视图模型。</param>
    public sealed partial record ShowPage(string Title, object ViewModel) : AppShellIntent;
}
