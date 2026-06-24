using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件变更。
/// </summary>
public abstract record HeaderMutation : IMviMutation<HeaderState>
{
    /// <summary>
    /// 表示设置标题的变更。
    /// </summary>
    /// <param name="Value">标题。</param>
    [MviMutation(Path = "Title")]
    public sealed record SetTitle(string Value) : HeaderMutation;

    /// <summary>
    /// 表示设置副标题的变更。
    /// </summary>
    /// <param name="Value">副标题。</param>
    [MviMutation(Path = "SubTitle")]
    public sealed record SetSubTitle(string Value) : HeaderMutation;
}
