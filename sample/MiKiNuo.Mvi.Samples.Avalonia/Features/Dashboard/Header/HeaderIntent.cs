using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件意图。
/// </summary>
public abstract partial record HeaderIntent : IMviIntent
{
    /// <summary>
    /// 表示更新标题意图。
    /// </summary>
    /// <param name="Title">标题。</param>
    /// <param name="SubTitle">副标题。</param>
    public sealed partial record UpdateTitle(string Title, string SubTitle) : HeaderIntent;
}
