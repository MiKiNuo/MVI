using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳意图处理器。
/// </summary>
public sealed class AppShellIntentHandler
    : IMviIntentHandler<AppShellState, AppShellIntent, AppShellMutation, AppShellEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<AppShellMutation, AppShellEffect>> HandleAsync(
        AppShellState state,
        AppShellIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<AppShellMutation, AppShellEffect> result = intent switch
        {
            AppShellIntent.ShowPage showPage => HandleShowPage(showPage),
            _ => MviHandleResult.Empty<AppShellMutation, AppShellEffect>(),
        };
        return new ValueTask<MviHandleResult<AppShellMutation, AppShellEffect>>(result);
    }

    private static MviHandleResult<AppShellMutation, AppShellEffect> HandleShowPage(
        AppShellIntent.ShowPage intent)
    {
        return MviHandleResult.Mutations<AppShellMutation, AppShellEffect>(
            new AppShellMutation.SetCurrentPageKey(intent.PageKey),
            new AppShellMutation.SetTitle(intent.Title));
    }
}
