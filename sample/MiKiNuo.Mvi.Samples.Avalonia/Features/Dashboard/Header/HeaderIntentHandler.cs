using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件意图处理器。
/// </summary>
public sealed class HeaderIntentHandler
    : IMviIntentHandler<HeaderState, HeaderIntent, HeaderMutation, HeaderEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<HeaderMutation, HeaderEffect>> HandleAsync(
        HeaderState state,
        HeaderIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<HeaderMutation, HeaderEffect> result = intent switch
        {
            HeaderIntent.UpdateTitle updateTitle => HandleUpdateTitle(updateTitle),
            _ => MviHandleResult.Empty<HeaderMutation, HeaderEffect>(),
        };
        return new ValueTask<MviHandleResult<HeaderMutation, HeaderEffect>>(result);
    }

    private static MviHandleResult<HeaderMutation, HeaderEffect> HandleUpdateTitle(
        HeaderIntent.UpdateTitle intent)
    {
        return MviHandleResult.Mutations<HeaderMutation, HeaderEffect>(
            new HeaderMutation.SetTitle(intent.Title),
            new HeaderMutation.SetSubTitle(intent.SubTitle));
    }
}
