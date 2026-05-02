using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="SubTitle">副标题。</param>
public sealed record HeaderState(string Title, string SubTitle) : IMviState;
